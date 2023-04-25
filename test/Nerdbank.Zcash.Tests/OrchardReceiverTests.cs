﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class OrchardReceiverTests
{
	[Fact]
	public void Ctor()
	{
		byte[] d = new byte[88 / 8];
		byte[] pkd = new byte[256 / 8];
		d[1] = 1;
		pkd[1] = 2;
		OrchardReceiver receiver = new(d, pkd);
		Assert.Equal(d, receiver.D.ToArray());
		Assert.Equal(pkd, receiver.Pkd.ToArray());

		// Verify that a copy of the data has been made.
		d[0] = 3;
		Assert.Equal(0, receiver.D[0]);
	}

	[Fact]
	public void Ctor_ArgValidation()
	{
		Assert.Throws<ArgumentException>("d", () => new OrchardReceiver(new byte[1], new byte[256 / 8]));
		Assert.Throws<ArgumentException>("pkd", () => new OrchardReceiver(new byte[88 / 8], new byte[1]));
	}

	[Fact]
	public void Pool_Orchard() => Assert.Equal(Pool.Orchard, default(OrchardReceiver).Pool);

	[Fact]
	public void UnifiedReceiverTypeCode() => Assert.Equal(0x03, OrchardReceiver.UnifiedReceiverTypeCode);
}
