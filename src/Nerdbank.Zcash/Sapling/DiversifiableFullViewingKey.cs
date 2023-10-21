// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.Zcash.Sapling;

/// <summary>
/// A viewing key that can decrypt incoming and outgoing transactions
/// and generate addresses.
/// </summary>
[DebuggerDisplay($"{{{nameof(DebuggerDisplay)},nq}}")]
public class DiversifiableFullViewingKey : FullViewingKey, IFullViewingKey, IUnifiedEncodingElement, IEquatable<DiversifiableFullViewingKey>, IKeyWithTextEncoding
{
	private const string Bech32MainNetworkHRP = "zdviews";
	private const string Bech32TestNetworkHRP = "zdviewtestsapling";
	private readonly DiversifierKey dk;
	private string? textEncoding;

	/// <summary>
	/// Initializes a new instance of the <see cref="DiversifiableFullViewingKey"/> class.
	/// </summary>
	/// <param name="fullViewingKey">The full viewing key.</param>
	/// <param name="dk">The diversifier key.</param>
	internal DiversifiableFullViewingKey(FullViewingKey fullViewingKey, in DiversifierKey dk)
		: base(fullViewingKey.Ak, fullViewingKey.Nk, fullViewingKey.IncomingViewingKey, fullViewingKey.Ovk)
	{
		this.dk = dk;

		// Replace the base class's value with our own that includes the Dk value.
		this.IncomingViewingKey = new DiversifiableIncomingViewingKey(base.IncomingViewingKey.Ivk.Value, this.Dk.Value, fullViewingKey.Network);
	}

	/// <summary>
	/// Gets the same key, but with the diversifier key removed.
	/// </summary>
	public FullViewingKey WithoutDiversifier => new(this.Ak, this.Nk, this.IncomingViewingKey.WithoutDiversifier, this.Ovk);

	/// <inheritdoc/>
	byte IUnifiedEncodingElement.UnifiedTypeCode => UnifiedTypeCodes.Sapling;

	/// <inheritdoc/>
	int IUnifiedEncodingElement.UnifiedDataLength => this.Ak.Value.Length + this.Nk.Value.Length + this.Ovk.Value.Length + this.Dk.Value.Length;

	/// <inheritdoc cref="IKeyWithTextEncoding.TextEncoding" />
	public new string TextEncoding
	{
		get
		{
			if (this.textEncoding is null)
			{
				Span<byte> encodedBytes = stackalloc byte[128];
				Span<char> encodedChars = stackalloc char[512];
				int byteLength = this.Encode(encodedBytes);
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

	/// <inheritdoc/>
	IIncomingViewingKey IFullViewingKey.IncomingViewingKey => this.IncomingViewingKey;

	/// <summary>
	/// Gets or sets the incoming viewing key.
	/// </summary>
	public new DiversifiableIncomingViewingKey IncomingViewingKey { get; protected set; }

	/// <summary>
	/// Gets the diversifier key.
	/// </summary>
	/// <value>A 32-byte buffer.</value>
	internal ref readonly DiversifierKey Dk => ref this.dk;

	private string DebuggerDisplay => this.IncomingViewingKey.DefaultAddress;

	/// <inheritdoc cref="IKeyWithTextEncoding.TryDecode(string, out DecodeError?, out string?, out IKeyWithTextEncoding?)"/>
	static bool IKeyWithTextEncoding.TryDecode(string encoding, [NotNullWhen(false)] out DecodeError? decodeError, [NotNullWhen(false)] out string? errorMessage, [NotNullWhen(true)] out IKeyWithTextEncoding? key)
	{
		if (TryDecode(encoding, out decodeError, out errorMessage, out DiversifiableFullViewingKey? fvk))
		{
			key = fvk;
			return true;
		}

		key = null;
		return false;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DiversifiableFullViewingKey"/> class
	/// from the bech32 encoding of an full viewing key as specified in ZIP-32.
	/// </summary>
	/// <inheritdoc cref="IKeyWithTextEncoding.TryDecode(string, out DecodeError?, out string?, out IKeyWithTextEncoding?)"/>
	public static bool TryDecode(string encoding, [NotNullWhen(false)] out DecodeError? decodeError, [NotNullWhen(false)] out string? errorMessage, [NotNullWhen(true)] out DiversifiableFullViewingKey? key)
	{
		Span<char> hrp = stackalloc char[50];
		Span<byte> data = stackalloc byte[128];
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

		key = new(
			Decode(data[..length.DataLength], network.Value),
			new DiversifierKey(data[96..length.DataLength]));
		return true;
	}

	/// <inheritdoc/>
	int IUnifiedEncodingElement.WriteUnifiedData(Span<byte> destination)
	{
		int written = 0;
		written += this.Ak.Value.CopyToRetLength(destination[written..]);
		written += this.Nk.Value.CopyToRetLength(destination[written..]);
		written += this.Ovk.Value.CopyToRetLength(destination[written..]);
		written += this.Dk.Value.CopyToRetLength(destination[written..]);
		return written;
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
		switch (NativeMethods.DecryptSaplingDiversifier(this.ToBytes().Value, this.Dk.Value, receiver.Span, diversifierSpan, out _))
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

	/// <summary>
	/// Derives the internal full viewing key from this.
	/// </summary>
	/// <returns>The internal viewing key.</returns>
	/// <remarks>
	/// This method assumes that <em>this</em> viewing key is the public facing one.
	/// The caller should take care to not call this method on what is already the internal key.
	/// </remarks>
	public DiversifiableFullViewingKey DeriveInternal()
	{
		Span<byte> publicFvk = stackalloc byte[96];
		base.Encode(publicFvk);

		Span<byte> internalFvk = stackalloc byte[96];
		Span<byte> internalDk = stackalloc byte[32];

		int result = NativeMethods.DeriveSaplingInternalFullViewingKey(publicFvk, this.Dk.Value, internalFvk, internalDk);
		if (result != 0)
		{
			throw new InvalidKeyException("Unable to derive internal full viewing key.");
		}

		return new(Decode(internalFvk, this.Network), new DiversifierKey(internalDk));
	}

	/// <inheritdoc/>
	public bool Equals(DiversifiableFullViewingKey? other)
	{
		return other is not null
			&& base.Equals(other)
			&& this.Dk.Value.SequenceEqual(other.Dk.Value);
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj) => obj is DiversifiableFullViewingKey other && this.Equals(other);

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		HashCode result = default;
		result.Add(base.GetHashCode());
		result.AddBytes(this.Dk.Value);
		return result.ToHashCode();
	}

	/// <inheritdoc cref="Orchard.FullViewingKey.DecodeUnifiedViewingKeyContribution(ReadOnlySpan{byte}, ZcashNetwork)"/>
	internal static IUnifiedEncodingElement DecodeUnifiedViewingKeyContribution(ReadOnlySpan<byte> keyContribution, ZcashNetwork network)
	{
		ReadOnlySpan<byte> ak = keyContribution[..32];
		ReadOnlySpan<byte> nk = keyContribution[32..64];
		ReadOnlySpan<byte> ovk = keyContribution[64..96];
		ReadOnlySpan<byte> dk = keyContribution[96..];
		IncomingViewingKey ivk = DiversifiableIncomingViewingKey.FromFullViewingKey(ak, nk, dk, network);
		FullViewingKey fvk = new(new(ak), new(nk), ivk, new(ovk));
		return new DiversifiableFullViewingKey(fvk, new(dk));
	}

	/// <summary>
	/// Gets the raw encoding.
	/// </summary>
	/// <param name="rawEncoding">Receives the raw encoding. Must be at least 96 bytes in length.</param>
	/// <returns>The number of bytes written to <paramref name="rawEncoding"/>. Always 128.</returns>
	/// <remarks>
	/// As specified in the <see href="https://github.com/zcash/zips/issues/727">ZIP-32 future edit</see>.
	/// </remarks>
	internal new int Encode(Span<byte> rawEncoding)
	{
		int written = 0;
		written += base.Encode(rawEncoding[written..]);
		written += this.Dk.Value.CopyToRetLength(rawEncoding[written..]);
		return written;
	}
}
