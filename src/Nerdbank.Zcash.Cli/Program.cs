﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Reflection;
using Nerdbank.Zcash.Cli;

Command rootCommand = new(Assembly.GetExecutingAssembly().GetName().Name!, Strings.RootCommandDescription)
{
	NewAccountCommand.BuildCommand(),
	ImportAccountCommand.BuildCommand(),
	UACommand.BuildCommand(),
	SyncCommand.BuildCommand(),
	BalanceCommand.BuildCommand(),
	HistoryCommand.BuildCommand(),
	SendCommand.BuildCommand(),
	RequestPaymentCommand.BuildCommand(),
};

await new CommandLineBuilder(rootCommand)
	.UseDefaults()
	.Build()
	.InvokeAsync(args);
