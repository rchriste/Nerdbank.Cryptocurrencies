﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nerdbank.Zcash.Sapling;

namespace Nerdbank.Zcash;

public partial class Zip32HDWallet
{
	/// <summary>
	/// Contains types and methods related to the Sapling pool.
	/// </summary>
	public static partial class Sapling
	{
		/// <inheritdoc cref="Create(ReadOnlySpan{byte}, ZcashNetwork)"/>
		/// <param name="mnemonic">The mnemonic phrase from which to generate the master key.</param>
		/// <param name="network"><inheritdoc cref="Create(ReadOnlySpan{byte}, ZcashNetwork)" path="/param[@name='network']"/></param>
		public static ExtendedSpendingKey Create(Bip39Mnemonic mnemonic, ZcashNetwork network) => Create(Requires.NotNull(mnemonic).Seed, network);

		/// <summary>
		/// Creates a master key for the Sapling pool.
		/// </summary>
		/// <param name="seed">The seed byte sequence, which MUST be at least 32 and at most 252 bytes.</param>
		/// <param name="network">The network this key should be used with.</param>
		/// <returns>The master extended spending key.</returns>
		public static ExtendedSpendingKey Create(ReadOnlySpan<byte> seed, ZcashNetwork network)
		{
			Span<byte> blakeOutput = stackalloc byte[64]; // 512 bits
			Blake2B.ComputeHash(seed, blakeOutput, new Blake2B.Config { Personalization = "ZcashIP32Sapling"u8, OutputSizeInBytes = blakeOutput.Length });
			ReadOnlySpan<byte> spendingKey = blakeOutput[..32];
			ChainCode chainCode = new(blakeOutput[32..]);

			return new ExtendedSpendingKey(new(spendingKey, network), chainCode, default, 0, 0)
			{
				DerivationPath = Bip32HDWallet.KeyPath.Root,
			};
		}

		/// <summary>
		/// Gets the fingerprint for this key.
		/// </summary>
		/// <param name="fullViewingKey">The full viewing key for which to produce a fingerprint.</param>
		/// <returns>The fingerprint.</returns>
		internal static FullViewingKeyFingerprint GetFingerprint(FullViewingKey fullViewingKey)
		{
			Requires.NotNull(fullViewingKey);

			Span<byte> fingerprint = stackalloc byte[32];
			Blake2B.ComputeHash(fullViewingKey.ToBytes().Value, fingerprint, new Blake2B.Config { Personalization = "ZcashSaplingFVFP"u8, OutputSizeInBytes = 32 });
			return new(fingerprint);
		}
	}
}
