﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Nerdbank.Cryptocurrencies;

namespace Nerdbank.Zcash.FixedLengthStructs;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1600 // Elements should be documented

internal unsafe struct Bytes4
{
	private const int Length = 4;
	private fixed byte value[Length];

	internal Bytes4(ReadOnlySpan<byte> value)
	{
		value.CopyToWithLengthCheck(this.ValueWritable);
	}

	[UnscopedRef]
	internal readonly ReadOnlySpan<byte> Value => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this.value[0]), Length);

	[UnscopedRef]
	private Span<byte> ValueWritable => MemoryMarshal.CreateSpan(ref this.value[0], Length);
}

internal unsafe struct Bytes11
{
	private const int Length = 11;
	private fixed byte value[Length];

	internal Bytes11(ReadOnlySpan<byte> value)
	{
		value.CopyToWithLengthCheck(this.ValueWritable);
	}

	[UnscopedRef]
	internal readonly ReadOnlySpan<byte> Value => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this.value[0]), Length);

	[UnscopedRef]
	internal Span<byte> ValueWritable => MemoryMarshal.CreateSpan(ref this.value[0], Length);
}

internal unsafe struct Bytes20
{
	private const int Length = 20;
	private fixed byte value[Length];

	internal Bytes20(ReadOnlySpan<byte> value)
	{
		value.CopyToWithLengthCheck(this.ValueWritable);
	}

	[UnscopedRef]
	internal readonly ReadOnlySpan<byte> Value => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this.value[0]), Length);

	[UnscopedRef]
	internal Span<byte> ValueWritable => MemoryMarshal.CreateSpan(ref this.value[0], Length);
}

internal unsafe struct Bytes32
{
	private const int Length = 32;
	private fixed byte value[Length];

	internal Bytes32(ReadOnlySpan<byte> value)
	{
		value.CopyToWithLengthCheck(this.ValueWritable);
	}

	[UnscopedRef]
	internal readonly ReadOnlySpan<byte> Value => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this.value[0]), Length);

	[UnscopedRef]
	private Span<byte> ValueWritable => MemoryMarshal.CreateSpan(ref this.value[0], Length);
}

internal unsafe struct Bytes64
{
	private const int Length = 64;
	private fixed byte value[Length];

	internal Bytes64(ReadOnlySpan<byte> value)
	{
		value.CopyToWithLengthCheck(this.ValueWritable);
	}

	[UnscopedRef]
	internal readonly ReadOnlySpan<byte> Value => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this.value[0]), Length);

	[UnscopedRef]
	private Span<byte> ValueWritable => MemoryMarshal.CreateSpan(ref this.value[0], Length);
}

internal unsafe struct Bytes80
{
	private const int Length = 80;
	private fixed byte value[Length];

	internal Bytes80(ReadOnlySpan<byte> value)
	{
		value.CopyToWithLengthCheck(this.ValueWritable);
	}

	[UnscopedRef]
	internal readonly ReadOnlySpan<byte> Value => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this.value[0]), Length);

	[UnscopedRef]
	private Span<byte> ValueWritable => MemoryMarshal.CreateSpan(ref this.value[0], Length);
}

internal unsafe struct Bytes96
{
	private const int Length = 96;
	private fixed byte value[Length];

	internal Bytes96(ReadOnlySpan<byte> value)
	{
		value.CopyToWithLengthCheck(this.ValueWritable);
	}

	[UnscopedRef]
	internal readonly ReadOnlySpan<byte> Value => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this.value[0]), Length);

	[UnscopedRef]
	private Span<byte> ValueWritable => MemoryMarshal.CreateSpan(ref this.value[0], Length);
}

internal unsafe struct Bytes192
{
	private const int Length = 192;
	private fixed byte value[Length];

	internal Bytes192(ReadOnlySpan<byte> value)
	{
		value.CopyToWithLengthCheck(this.ValueWritable);
	}

	[UnscopedRef]
	internal readonly ReadOnlySpan<byte> Value => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this.value[0]), Length);

	[UnscopedRef]
	private Span<byte> ValueWritable => MemoryMarshal.CreateSpan(ref this.value[0], Length);
}

internal unsafe struct Bytes296
{
	private const int Length = 296;
	private fixed byte value[Length];

	internal Bytes296(ReadOnlySpan<byte> value)
	{
		value.CopyToWithLengthCheck(this.ValueWritable);
	}

	[UnscopedRef]
	internal readonly ReadOnlySpan<byte> Value => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this.value[0]), Length);

	[UnscopedRef]
	private Span<byte> ValueWritable => MemoryMarshal.CreateSpan(ref this.value[0], Length);
}

internal unsafe struct Bytes512
{
	private const int Length = 512;
	private fixed byte value[Length];

	internal Bytes512(ReadOnlySpan<byte> value, bool allowShorterInput = false)
	{
		value.CopyToWithLengthCheck(this.ValueWritable, allowShorterInput: allowShorterInput);
	}

	[UnscopedRef]
	internal readonly ReadOnlySpan<byte> Value => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this.value[0]), Length);

	[UnscopedRef]
	internal Span<byte> ValueWritable => MemoryMarshal.CreateSpan(ref this.value[0], Length);
}

internal unsafe struct Bytes580
{
	private const int Length = 580;
	private fixed byte value[Length];

	internal Bytes580(ReadOnlySpan<byte> value, bool allowShorterInput = false)
	{
		value.CopyToWithLengthCheck(this.ValueWritable, allowShorterInput: allowShorterInput);
	}

	[UnscopedRef]
	internal readonly ReadOnlySpan<byte> Value => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this.value[0]), Length);

	[UnscopedRef]
	internal Span<byte> ValueWritable => MemoryMarshal.CreateSpan(ref this.value[0], Length);
}

internal unsafe struct Bytes1202
{
	private const int Length = 1202;
	private fixed byte value[Length];

	internal Bytes1202(ReadOnlySpan<byte> value, bool allowShorterInput = false)
	{
		value.CopyToWithLengthCheck(this.ValueWritable, allowShorterInput: allowShorterInput);
	}

	[UnscopedRef]
	internal readonly ReadOnlySpan<byte> Value => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this.value[0]), Length);

	[UnscopedRef]
	internal Span<byte> ValueWritable => MemoryMarshal.CreateSpan(ref this.value[0], Length);
}
