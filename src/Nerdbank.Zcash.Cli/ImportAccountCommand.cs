﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.IO;

namespace Nerdbank.Zcash.Cli;

internal class ImportAccountCommand
{
	private ImportAccountCommand()
	{
	}

	internal required IConsole Console { get; set; }

	internal required string Key { get; set; }

	internal Uri? LightWalletServerUrl { get; set; }

	internal ulong? BirthdayHeight { get; set; }

	internal string? WalletPath { get; set; }

	internal static Command BuildCommand()
	{
		Argument<string> keyArgument = new("key", Strings.KeyArgumentDescription) { Arity = ArgumentArity.ExactlyOne };
		Option<string> walletPathOption = new Option<string>("--wallet", Strings.NewAccountWalletPathOptionDescription)
			.LegalFilePathsOnly();
		Option<ulong> birthdayHeightOption = new("--birthday-height", Strings.BirthdayHeightOptionDescription);
		Option<Uri> lightServerUriOption = new("--lightserverUrl", Strings.LightServerUrlOptionDescription);

		Command command = new("ImportAccount", Strings.ImportAccountCommandDescription)
		{
			keyArgument,
			walletPathOption,
			birthdayHeightOption,
			lightServerUriOption,
		};

		command.SetHandler(ctxt =>
		{
			ctxt.ExitCode = new ImportAccountCommand()
			{
				Console = ctxt.Console,
				Key = ctxt.ParseResult.GetValueForArgument(keyArgument),
				LightWalletServerUrl = ctxt.ParseResult.GetValueForOption(lightServerUriOption),
				WalletPath = ctxt.ParseResult.GetValueForOption(walletPathOption),
				BirthdayHeight = ctxt.ParseResult.GetValueForOption(birthdayHeightOption),
			}.Execute(ctxt.GetCancellationToken());
		});

		return command;
	}

	private int Execute(CancellationToken cancellationToken)
	{
		if (!ZcashAccount.TryImportAccount(this.Key, out ZcashAccount? account))
		{
			this.Console.Error.WriteLine(Strings.UnrecognizedKeyFormat);
			return 1;
		}

		account.BirthdayHeight = this.BirthdayHeight;

		this.Console.WriteLine($"Network: {account.Network}");

		Uri serverUrl = this.LightWalletServerUrl ?? Utilities.GetDefaultLightWalletUrl(account.Network == ZcashNetwork.TestNet);
		if (this.WalletPath is not null)
		{
			using LightWalletClient client = Utilities.ConstructLightClient(
				this.LightWalletServerUrl,
				account,
				this.WalletPath,
				watchMemPool: false);
			this.Console.WriteLine($"Wallet file saved to \"{this.WalletPath}\".");
		}

		Utilities.PrintAccountInfo(this.Console, account);

		this.Console.WriteLine(string.Empty);

		return 0;
	}
}
