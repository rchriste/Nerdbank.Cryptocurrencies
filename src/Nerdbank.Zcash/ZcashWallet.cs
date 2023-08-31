﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Nerdbank.Zcash.Zip32HDWallet;

namespace Nerdbank.Zcash;

/// <summary>
/// A high-level API for creating, managing and using Zcash accounts.
/// </summary>
[DebuggerDisplay($"{{{nameof(DebuggerDisplay)},nq}}")]
public class ZcashWallet
{
	private readonly Zip32HDWallet zip32;
	private readonly SortedDictionary<uint, Account> accounts = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="ZcashWallet"/> class.
	/// </summary>
	/// <param name="mnemonic">The mnemonic that will always initialize the same wallet with access to the same funds.</param>
	/// <param name="network">The network the wallet operates on.</param>
	public ZcashWallet(Bip39Mnemonic mnemonic, ZcashNetwork network)
	{
		this.zip32 = new Zip32HDWallet(mnemonic, network);
		this.AddAccount();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ZcashWallet"/> class.
	/// </summary>
	/// <param name="seed">The seed to use as the master keys, from which accounts are derived. The same seed will always generate the same accounts.</param>
	/// <param name="network">The network this wallet should be used on.</param>
	public ZcashWallet(ReadOnlySpan<byte> seed, ZcashNetwork network)
	{
		this.zip32 = new Zip32HDWallet(seed, network);
		this.AddAccount();
	}

	/// <inheritdoc cref="Zip32HDWallet.Network"/>
	public ZcashNetwork Network => this.zip32.Network;

	/// <inheritdoc cref="Zip32HDWallet.Mnemonic"/>
	public Bip39Mnemonic? Mnemonic => this.zip32.Mnemonic;

	/// <inheritdoc cref="Zip32HDWallet.Seed"/>
	public ReadOnlyMemory<byte> Seed => this.zip32.Seed;

	/// <summary>
	/// Gets the dictionary of accounts, keyed by their index.
	/// </summary>
	/// <remarks>
	/// Enumerating the dictionary will always produce the accounts in order of their index.
	/// </remarks>
	public IReadOnlyDictionary<uint, Account> Accounts => this.accounts;

	private string DebuggerDisplay => $"Zcash wallet: {this.zip32.Mnemonic?.ToString(2)}";

	/// <summary>
	/// Adds a new account to the wallet.
	/// </summary>
	/// <param name="index">The index of the account to create. For a given wallet (mnemonic), a given index will always recreate the same account with access to the same funds.</param>
	/// <returns>The newly created account.</returns>
	/// <remarks>
	/// The new account will be added to the <see cref="Accounts"/> collection.
	/// </remarks>
	public Account AddAccount(uint? index = null)
	{
		if (index is null)
		{
			for (index = 0; index < uint.MaxValue && this.accounts.ContainsKey(index.Value); index++)
			{
			}
		}
		else
		{
			Verify.Operation(!this.accounts.ContainsKey(index.Value), "The account with that index already exists.");
		}

		Account account = new(this, index.Value);
		this.accounts.Add(index.Value, account);
		return account;
	}

	/// <summary>
	/// Describes a single Zcash account, with unique spending authorities.
	/// </summary>
	[DebuggerDisplay($"{{{nameof(DebuggerDisplay)},nq}}")]
	public class Account
	{
		/// <summary>
		/// The wallet that owns this account.
		/// </summary>
		private readonly ZcashWallet owner;

		/// <summary>
		/// The number of transparent addresses that are likely to have been generated.
		/// </summary>
		/// <remarks>
		/// TODO: This number should be generated from a call to <see cref="Bip44MultiAccountHD.DiscoverUsedAccountsAsync(uint, Func{Bip32HDWallet.KeyPath, ValueTask{bool}}, uint)"/>.
		/// </remarks>
		private uint transparentAddressesToScanAsync = Bip44MultiAccountHD.RecommendedAddressGapLimit;

		/// <summary>
		/// Initializes a new instance of the <see cref="Account"/> class.
		/// </summary>
		/// <param name="owner">The owning wallet.</param>
		/// <param name="index">The account index within the wallet.</param>
		internal Account(ZcashWallet owner, uint index)
		{
			this.owner = owner;
			this.Index = index;

			Transparent.ExtendedSpendingKey transparent = owner.zip32.CreateTransparentAccount(index);
			Zip32HDWallet.Sapling.ExtendedSpendingKey sapling = owner.zip32.CreateSaplingAccount(index);
			Zip32HDWallet.Orchard.ExtendedSpendingKey orchard = owner.zip32.CreateOrchardAccount(index);

			this.Spending = new SpendingKeys(transparent, sapling, orchard);
			this.FullViewing = this.Spending.FullViewingKey;
			this.IncomingViewing = this.FullViewing.IncomingViewingKey;
		}

		/// <summary>
		/// Gets the derivation index of this account.
		/// </summary>
		/// <remarks>
		/// This is typically 0 for the first account, with increasing indexes for each additional account created from the same wallet/mnemonic.
		/// </remarks>
		public uint Index { get; }

		/// <summary>
		/// Gets the network this account should be used with.
		/// </summary>
		public ZcashNetwork Network => this.owner.Network;

		/// <summary>
		/// Gets the spending keys, if this account has access to them.
		/// </summary>
		public SpendingKeys? Spending { get; }

		/// <summary>
		/// Gets the full viewing keys, if this account has access to them.
		/// </summary>
		public FullViewingKeys? FullViewing { get; }

		/// <summary>
		/// Gets the incoming viewing keys.
		/// </summary>
		public IncomingViewingKeys IncomingViewing { get; }

		/// <summary>
		/// Gets the default unified address for this account.
		/// </summary>
		public UnifiedAddress DefaultAddress => this.IncomingViewing.UnifiedKey.DefaultAddress;

		private string DebuggerDisplay => $"{this.Index}: {this.DefaultAddress}";

		/// <summary>
		/// Gets a diversifier index that is unique to this moment in time,
		/// for use as an input to the <see cref="GetDiversifiedAddress(ref DiversifierIndex)"/> method.
		/// </summary>
		/// <returns>The diversifier index.</returns>
		public static DiversifierIndex GetTimeBasedDiversifier() => new(DateTime.UtcNow.Ticks);

		/// <inheritdoc cref="GetDiversifiedAddress(ref DiversifierIndex)"/>
		public UnifiedAddress GetDiversifiedAddress()
		{
			DiversifierIndex index = GetTimeBasedDiversifier();
			return this.GetDiversifiedAddress(ref index);
		}

		/// <summary>
		/// Gets a unique address that sends ZEC to this account but cannot be correlated with other addresses
		/// except using the <see cref="IncomingViewing"/> key.
		/// </summary>
		/// <param name="diversifierIndex">
		/// The 11-byte diversifier index to start searching at, in the range of 0..(2^88 - 1).
		/// Not every index will produce a valid diversifier. About half will fail.
		/// The default diversifier is defined as the smallest non-negative index that produces a valid diversifier.
		/// This value will be incremented until a diversifier can be found, considering the buffer to be a little-endian encoded integer.
		/// </param>
		/// <returns>
		/// The diversified address. This will <em>not</em> include a transparent pool receiver because it does not support diversifiers.
		/// </returns>
		/// <remarks>
		/// By giving a unique diversified address to each person that sends you ZEC,
		/// you prevent them from being able to correlate this address with others to discover they are
		/// interacting with the same person, thereby increasing your privacy.
		/// </remarks>
		public UnifiedAddress GetDiversifiedAddress(ref DiversifierIndex diversifierIndex)
		{
			List<ZcashAddress> componentAddresses = new(2);

			// Now get the closest matching Sapling diversifier we can.
			if (this.IncomingViewing.Sapling is not null)
			{
				Verify.Operation(this.IncomingViewing.Sapling.TryCreateReceiver(ref diversifierIndex, out SaplingReceiver? sapling), "No sapling diversifier could be found at or above that index.");
				componentAddresses.Add(new SaplingAddress(sapling.Value, this.owner.Network));
			}

			// The orchard diversifier always works.
			if (this.IncomingViewing.Orchard is not null)
			{
				OrchardReceiver orchard = this.IncomingViewing.Orchard.CreateReceiver(diversifierIndex);
				componentAddresses.Add(new OrchardAddress(orchard, this.owner.Network));
			}

			Verify.Operation(componentAddresses.Count > 0, "This account doesn't include any diversifiable keys.");

			return UnifiedAddress.Create(componentAddresses);
		}

		/// <summary>
		/// Checks whether a given address sends ZEC to this account.
		/// </summary>
		/// <param name="address">The address to test.</param>
		/// <returns><see langword="true" /> if all receivers in <paramref name="address"/> are confirmed to direct ZEC to this account; <see langword="false" /> otherwise.</returns>
		/// <remarks>
		/// There is a risk that a unified address containing multiple receivers may have been
		/// contrived to include receivers from this account and other receivers <em>not</em> belonging to this account.
		/// To avoid being tricked into reusing such a contrived address and unwittingly diverting ZEC to someone else's wallet,
		/// <see langword="false"/> is returned if any receiver does not belong to this account.
		/// </remarks>
		public bool AddressSendsToThisAcount(ZcashAddress address)
		{
			Requires.NotNull(address);

			if (address is UnifiedAddress ua)
			{
				if (ua.Receivers.Count == 0)
				{
					return false;
				}

				foreach (ZcashAddress individualAddress in ua.Receivers)
				{
					if (!TestAddress(individualAddress))
					{
						return false;
					}
				}

				return true;
			}
			else
			{
				return TestAddress(address);
			}

			bool TestAddress(ZcashAddress individualAddress)
			{
				if (this.IncomingViewing.Orchard is not null && individualAddress.GetPoolReceiver<OrchardReceiver>() is { } orchardReceiver && this.IncomingViewing.Orchard.CheckReceiver(orchardReceiver))
				{
					return true;
				}

				if (this.IncomingViewing.Sapling is not null && individualAddress.GetPoolReceiver<SaplingReceiver>() is { } saplingReceiver && this.IncomingViewing.Sapling.CheckReceiver(saplingReceiver))
				{
					return true;
				}

				Transparent.ExtendedViewingKey? transparentViewing = this.FullViewing?.Transparent ?? this.IncomingViewing.Transparent;
				if (transparentViewing is not null)
				{
					if (individualAddress.GetPoolReceiver<TransparentP2PKHReceiver>() is { } p2pkhReceiver && transparentViewing.CheckReceiver(p2pkhReceiver, this.transparentAddressesToScanAsync))
					{
						return true;
					}

					if (individualAddress.GetPoolReceiver<TransparentP2SHReceiver>() is { } p2shReceiver && transparentViewing.CheckReceiver(p2shReceiver, this.transparentAddressesToScanAsync))
					{
						return true;
					}
				}

				return false;
			}
		}
	}

	/// <summary>
	/// Spending keys for each pool.
	/// </summary>
	public record SpendingKeys : ISpendingKey
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SpendingKeys"/> class.
		/// </summary>
		/// <param name="transparent">A key for the transparent pool.</param>
		/// <param name="sapling">A key for the sapling pool.</param>
		/// <param name="orchard">A key for the orchard pool.</param>
		internal SpendingKeys(
			Transparent.ExtendedSpendingKey transparent,
			Zip32HDWallet.Sapling.ExtendedSpendingKey sapling,
			Zip32HDWallet.Orchard.ExtendedSpendingKey orchard)
		{
			this.Transparent = transparent;
			this.Sapling = sapling;
			this.Orchard = orchard;
			this.UnifiedKey = UnifiedSpendingKey.Create(transparent, sapling, orchard.SpendingKey);

			this.FullViewingKey = new FullViewingKeys(transparent.FullViewingKey, sapling.FullViewingKey, orchard.FullViewingKey);
		}

		/// <summary>
		/// Gets the unified spending key for this account.
		/// </summary>
		internal UnifiedSpendingKey UnifiedKey { get; }

		/// <summary>
		/// Gets the spending key for the transparent pool (<c>m/44'/133'/account'</c>).
		/// </summary>
		public Transparent.ExtendedSpendingKey? Transparent { get; }

		/// <summary>
		/// Gets the spending key for the sapling pool.
		/// </summary>
		public Zip32HDWallet.Sapling.ExtendedSpendingKey? Sapling { get; }

		/// <summary>
		/// Gets the spending key for the orchard pool.
		/// </summary>
		public Zip32HDWallet.Orchard.ExtendedSpendingKey? Orchard { get; }

		/// <summary>
		/// Gets the full viewing key.
		/// </summary>
		internal FullViewingKeys FullViewingKey { get; }

		/// <inheritdoc/>
		IFullViewingKey ISpendingKey.FullViewingKey => this.FullViewingKey;
	}

	/// <summary>
	/// Full viewing keys for each pool.
	/// </summary>
	public record FullViewingKeys : IFullViewingKey
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FullViewingKeys"/> class.
		/// </summary>
		/// <param name="transparent">A key for the transparent pool.</param>
		/// <param name="sapling">A key for the sapling pool.</param>
		/// <param name="orchard">A key for the orchard pool.</param>
		internal FullViewingKeys(Transparent.ExtendedViewingKey transparent, Sapling.DiversifiableFullViewingKey sapling, Orchard.FullViewingKey orchard)
		{
			this.Transparent = transparent;
			this.Sapling = sapling;
			this.Orchard = orchard;
			this.UnifiedKey = UnifiedViewingKey.Full.Create(transparent, sapling, orchard);

			this.IncomingViewingKey = new IncomingViewingKeys(transparent.IncomingViewingKey, sapling.IncomingViewingKey, orchard.IncomingViewingKey);
		}

		/// <summary>
		/// Gets the unified full viewing key for this account.
		/// </summary>
		public UnifiedViewingKey.Full UnifiedKey { get; }

		/// <summary>
		/// Gets the full viewing key for the transparent pool (<c>m/44'/133'/account'</c>).
		/// </summary>
		public Transparent.ExtendedViewingKey? Transparent { get; }

		/// <summary>
		/// Gets the full viewing key for the sapling pool.
		/// </summary>
		public Sapling.DiversifiableFullViewingKey? Sapling { get; }

		/// <summary>
		/// Gets the full viewing key for the orchard pool.
		/// </summary>
		public Orchard.FullViewingKey? Orchard { get; }

		/// <summary>
		/// Gets the incoming viewing key.
		/// </summary>
		internal IncomingViewingKeys IncomingViewingKey { get; }

		/// <inheritdoc/>
		IIncomingViewingKey IFullViewingKey.IncomingViewingKey => this.IncomingViewingKey;
	}

	/// <summary>
	/// Incoming viewing keys for each pool.
	/// </summary>
	public record IncomingViewingKeys : IIncomingViewingKey
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IncomingViewingKeys"/> class.
		/// </summary>
		/// <param name="transparent">A key for the transparent pool.</param>
		/// <param name="sapling">A key for the sapling pool.</param>
		/// <param name="orchard">A key for the orchard pool.</param>
		public IncomingViewingKeys(Transparent.ExtendedViewingKey transparent, Sapling.IncomingViewingKey sapling, Orchard.IncomingViewingKey orchard)
		{
			this.Transparent = transparent;
			this.Sapling = sapling;
			this.Orchard = orchard;
			this.UnifiedKey = UnifiedViewingKey.Incoming.Create(transparent, sapling, orchard);
		}

		/// <summary>
		/// Gets the unified incoming viewing key for this account.
		/// </summary>
		public UnifiedViewingKey.Incoming UnifiedKey { get; }

		/// <summary>
		/// Gets the incoming viewing key for the transparent pool (<c>m/44'/133'/account'</c>).
		/// </summary>
		public Transparent.ExtendedViewingKey? Transparent { get; }

		/// <summary>
		/// Gets the incoming viewing key for the sapling pool.
		/// </summary>
		public Sapling.IncomingViewingKey? Sapling { get; }

		/// <summary>
		/// Gets the incoming viewing key for the orchard pool.
		/// </summary>
		public Orchard.IncomingViewingKey? Orchard { get; }

		/// <inheritdoc/>
		ZcashAddress IIncomingViewingKey.DefaultAddress => this.UnifiedKey.DefaultAddress;

		/// <inheritdoc/>
		ZcashNetwork IZcashKey.Network => this.UnifiedKey.Network;
	}
}
