﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.Zcash.Sapling;

/// <summary>
/// A viewing key that includes the diversifier key for incoming transactions.
/// </summary>
[DebuggerDisplay($"{{{nameof(DebuggerDisplay)},nq}}")]
public class DiversifiableIncomingViewingKey : IncomingViewingKey, IUnifiedEncodingElement, IIncomingViewingKey, IKeyWithTextEncoding
{
	private const string Bech32MainNetworkHRP = "zdivks";
	private const string Bech32TestNetworkHRP = "zdivktestsapling";
	private readonly DiversifierKey dk;
	private string? textEncoding;

	/// <summary>
	/// Initializes a new instance of the <see cref="DiversifiableIncomingViewingKey"/> class.
	/// </summary>
	/// <param name="ivk">The 32-byte ivk value.</param>
	/// <param name="dk">The 11-byte diversification key.</param>
	/// <param name="network">The network this key should be used with.</param>
	internal DiversifiableIncomingViewingKey(ReadOnlySpan<byte> ivk, ReadOnlySpan<byte> dk, ZcashNetwork network)
		: base(ivk, network)
	{
		this.dk = new(dk);
	}

	/// <summary>
	/// Gets the same key, but with the diversifier key removed.
	/// </summary>
	public IncomingViewingKey WithoutDiversifier => new(this.Ivk.Value, this.Network);

	/// <inheritdoc/>
	ZcashAddress IIncomingViewingKey.DefaultAddress => this.DefaultAddress;

	/// <summary>
	/// Gets the default address for this spending key.
	/// </summary>
	/// <remarks>
	/// Create additional diversified addresses using <see cref="TryCreateReceiver"/>.
	/// </remarks>
	public SaplingAddress DefaultAddress => new(this.CreateDefaultReceiver(), this.Network);

	/// <inheritdoc/>
	byte IUnifiedEncodingElement.UnifiedTypeCode => UnifiedTypeCodes.Sapling;

	/// <inheritdoc/>
	int IUnifiedEncodingElement.UnifiedDataLength => 32 * 2;

	/// <inheritdoc cref="IKeyWithTextEncoding.TextEncoding" />
	public new string TextEncoding
	{
		get
		{
			if (this.textEncoding is null)
			{
				// As specified in https://github.com/zcash/zips/issues/727.
				Span<byte> encodedBytes = stackalloc byte[64];
				Span<char> encodedChars = stackalloc char[512];
				int byteLength = this.Dk.Value.CopyToRetLength(encodedBytes);
				byteLength += this.Ivk.Value.CopyToRetLength(encodedBytes[byteLength..]);
				string hrp = this.Network switch
				{
					ZcashNetwork.MainNet => Bech32MainNetworkHRP,
					ZcashNetwork.TestNet => Bech32TestNetworkHRP,
					_ => throw new NotSupportedException(),
				};
				int charLength = Bech32.Original.Encode(hrp, encodedBytes[..byteLength], encodedChars);
				this.textEncoding = new string(encodedChars[..charLength]);
			}

			return this.textEncoding;
		}
	}

	/// <summary>
	/// Gets the diversification key.
	/// </summary>
	internal ref readonly DiversifierKey Dk => ref this.dk;

	private string DebuggerDisplay => this.DefaultAddress;

	/// <inheritdoc cref="IKeyWithTextEncoding.TryDecode(string, out DecodeError?, out string?, out IKeyWithTextEncoding?)"/>
	static bool IKeyWithTextEncoding.TryDecode(string encoding, [NotNullWhen(false)] out DecodeError? decodeError, [NotNullWhen(false)] out string? errorMessage, [NotNullWhen(true)] out IKeyWithTextEncoding? key)
	{
		if (TryDecode(encoding, out decodeError, out errorMessage, out IncomingViewingKey? ivk))
		{
			key = ivk;
			return true;
		}

		key = null;
		return false;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DiversifiableIncomingViewingKey"/> class
	/// from the bech32 encoding of an incoming viewing key as specified in ZIP-32.
	/// </summary>
	/// <inheritdoc cref="IKeyWithTextEncoding.TryDecode(string, out DecodeError?, out string?, out IKeyWithTextEncoding?)"/>
	public static bool TryDecode(ReadOnlySpan<char> encoding, [NotNullWhen(false)] out DecodeError? decodeError, [NotNullWhen(false)] out string? errorMessage, [NotNullWhen(true)] out DiversifiableIncomingViewingKey? key)
	{
		Span<char> hrp = stackalloc char[50];
		Span<byte> data = stackalloc byte[64];
		if (!Bech32.Original.TryDecode(encoding, hrp, data, out decodeError, out errorMessage, out (int TagLength, int DataLength) length))
		{
			key = null;
			return false;
		}

		hrp = hrp[..length.TagLength];
		ZcashNetwork? network = hrp switch
		{
			Bech32MainNetworkHRP => ZcashNetwork.MainNet,
			Bech32TestNetworkHRP => ZcashNetwork.TestNet,
			_ => null,
		};
		if (network is null)
		{
			decodeError = DecodeError.UnrecognizedHRP;
			errorMessage = $"Unexpected bech32 tag: {hrp}";
			key = null;
			return false;
		}

		key = new DiversifiableIncomingViewingKey(data[32..length.DataLength], data[..32], network.Value);
		return true;
	}

	/// <inheritdoc/>
	int IUnifiedEncodingElement.WriteUnifiedData(Span<byte> destination)
	{
		int written = 0;
		written += this.Dk.Value.CopyToRetLength(destination[written..]);
		written += this.Ivk.Value.CopyToRetLength(destination[written..]);
		return written;
	}

	/// <summary>
	/// Creates a sapling receiver using this key and a given diversifier.
	/// </summary>
	/// <param name="diversifierIndex">
	/// The diversifier index to start searching at, in the range of 0..(2^88 - 1).
	/// Not every index will produce a valid diversifier. About half will fail.
	/// The default diversifier is defined as the smallest non-negative index that produces a valid diversifier.
	/// This value will be incremented until a diversifier can be found, considering the buffer to be a little-endian encoded integer.
	/// </param>
	/// <param name="receiver">Receives the sapling receiver, if successful.</param>
	/// <returns>
	/// <see langword="true"/> if a valid diversifier could be produced at or above the initial value given by <paramref name="diversifierIndex"/>.
	/// <see langword="false"/> if no valid diversifier could be found at or above <paramref name="diversifierIndex"/>.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="diversifierIndex"/> is negative.</exception>
	public bool TryCreateReceiver(ref DiversifierIndex diversifierIndex, [NotNullWhen(true)] out SaplingReceiver? receiver)
	{
		Span<byte> receiverBytes = stackalloc byte[SaplingReceiver.Length];
		Span<byte> diversifierIndexSpan = stackalloc byte[11];
		diversifierIndex.Value.CopyTo(diversifierIndexSpan);
		if (NativeMethods.TryGetSaplingReceiver(this.Ivk.Value, this.Dk.Value, diversifierIndexSpan, receiverBytes) != 0)
		{
			receiver = null;
			return false;
		}

		diversifierIndex = new(diversifierIndexSpan);
		receiver = new(receiverBytes);
		return true;
	}

	/// <summary>
	/// Creates the default sapling receiver for this key.
	/// </summary>
	/// <returns>The receiver.</returns>
	public SaplingReceiver CreateDefaultReceiver()
	{
		DiversifierIndex diversifierIndex = default;
		Assumes.True(this.TryCreateReceiver(ref diversifierIndex, out SaplingReceiver? receiver));
		return receiver.Value;
	}

	/// <summary>
	/// Checks whether a given sapling receiver was derived from the same spending authority as this key
	/// (in other words: would ZEC sent to this receiver arrive in this account?).
	/// </summary>
	/// <param name="receiver">The receiver to test.</param>
	/// <returns><see langword="true"/> if this receiver would send ZEC to this account; otherwise <see langword="false"/>.</returns>
	/// <remarks>
	/// <para>This is a simpler front-end for the <see cref="TryGetDiversifierIndex"/> method,
	/// which runs a similar test but also provides the decrypted diversifier index.</para>
	/// </remarks>
	public bool CheckReceiver(SaplingReceiver receiver) => this.TryGetDiversifierIndex(receiver, out _);

	/// <summary>
	/// Checks whether a given sapling receiver was derived from the same spending authority as this key
	/// (in other words: would ZEC sent to this receiver arrive in this account?).
	/// If so, the diversifier that was used to create it is decrypted back into its original index.
	/// </summary>
	/// <param name="receiver">The receiver to decrypt.</param>
	/// <param name="diversifierIndex">Receives the original diversifier index, if successful.</param>
	/// <returns>A value indicating whether the receiver could be decrypted successfully (i.e. the receiver came from this key).</returns>
	/// <remarks>
	/// <para>Use <see cref="CheckReceiver(SaplingReceiver)"/> for a simpler API if the diversifier index is not required.</para>
	/// </remarks>
	public bool TryGetDiversifierIndex(SaplingReceiver receiver, [NotNullWhen(true)] out DiversifierIndex? diversifierIndex)
	{
		Span<byte> diversifierSpan = stackalloc byte[11];
		switch (NativeMethods.DecryptSaplingDiversifierWithIvk(this.Ivk.Value, this.Dk.Value, receiver.Span, diversifierSpan))
		{
			case 0:
				diversifierIndex = new(diversifierSpan);
				return true;
			case 1:
				diversifierIndex = null;
				return false;
			default: throw new ArgumentException();
		}
	}

	/// <inheritdoc cref="Orchard.FullViewingKey.DecodeUnifiedViewingKeyContribution(ReadOnlySpan{byte}, ZcashNetwork)"/>
	internal static IUnifiedEncodingElement DecodeUnifiedViewingKeyContribution(ReadOnlySpan<byte> keyContribution, ZcashNetwork network)
	{
		Requires.Argument(keyContribution.Length == 64, nameof(keyContribution), "Unexpected length.");
		ReadOnlySpan<byte> dk = keyContribution[0..32];
		ReadOnlySpan<byte> ivk = keyContribution[32..64];
		return new DiversifiableIncomingViewingKey(ivk, dk, network);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DiversifiableIncomingViewingKey"/> class
	/// from elements of a full viewing key.
	/// </summary>
	/// <param name="ak">The Ak subgroup point.</param>
	/// <param name="nk">The nullifier deriving key.</param>
	/// <param name="dk">The diversification key. May be default. Required for inclusion in a unified viewing key.</param>
	/// <param name="network">The network on which this key should operate.</param>
	/// <returns>The constructed incoming viewing key.</returns>
	/// <exception cref="InvalidKeyException">Thrown if an error occurs while parsing the inputs.</exception>
	internal static DiversifiableIncomingViewingKey FromFullViewingKey(ReadOnlySpan<byte> ak, ReadOnlySpan<byte> nk, ReadOnlySpan<byte> dk, ZcashNetwork network)
	{
		Span<byte> ivk = stackalloc byte[32];
		if (NativeMethods.DeriveSaplingIncomingViewingKeyFromFullViewingKey(ak, nk, ivk) != 0)
		{
			throw new InvalidKeyException();
		}

		return new(ivk, dk, network);
	}
}
