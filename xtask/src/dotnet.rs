// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

use crate::util::{prepare_dotnet_env, run_command_quiet_with_env};
use std::process::ExitCode;

const FFI_PACKAGE: &str = "microsoft-webui-ffi";

pub fn run() -> ExitCode {
    let root = match std::env::current_dir() {
        Ok(path) => path,
        Err(error) => {
            eprintln!(
                "  {} failed to read current directory: {error}",
                console::style("✘").red().bold(),
            );
            return ExitCode::FAILURE;
        }
    };

    let solution = root.join("dotnet").join("Microsoft.WebUI.sln");
    let tests = root
        .join("dotnet")
        .join("test")
        .join("Microsoft.WebUI.Tests")
        .join("Microsoft.WebUI.Tests.csproj");
    let ffi_library = root
        .join("target")
        .join("release")
        .join(host_ffi_library_name());
    let mut dotnet_env = match prepare_dotnet_env(&root) {
        Ok(env) => env,
        Err(message) => {
            eprintln!("  {} {message}", console::style("✘").red().bold(),);
            return ExitCode::FAILURE;
        }
    };
    dotnet_env.push(("DOTNET_ROLL_FORWARD".to_string(), "LatestMajor".to_string()));

    eprintln!("\n{} dotnet\n", console::style("▸").cyan().bold());

    if let Err(message) = run_command_quiet_with_env(
        "cargo",
        &["build", "--release", "-p", FFI_PACKAGE],
        None,
        &[],
    ) {
        eprintln!("  {} build native ffi", console::style("✘").red().bold(),);
        print_failure_output(&message);
        return ExitCode::FAILURE;
    }
    eprintln!("  {} build native ffi", console::style("✔").green(),);

    if !ffi_library.is_file() {
        eprintln!(
            "  {} expected native library at {}",
            console::style("✘").red().bold(),
            ffi_library.display(),
        );
        return ExitCode::FAILURE;
    }

    let solution_string = solution.to_string_lossy().into_owned();
    if let Err(message) = run_command_quiet_with_env(
        "dotnet",
        &["build", &solution_string, "--configuration", "Release"],
        None,
        &dotnet_env,
    ) {
        eprintln!("  {} dotnet build", console::style("✘").red().bold(),);
        print_failure_output(&message);
        return ExitCode::FAILURE;
    }
    eprintln!("  {} dotnet build", console::style("✔").green(),);

    let tests_string = tests.to_string_lossy().into_owned();
    dotnet_env.push((
        "WEBUI_LIB_PATH".to_string(),
        ffi_library.to_string_lossy().into_owned(),
    ));
    if let Err(message) = run_command_quiet_with_env(
        "dotnet",
        &[
            "test",
            &tests_string,
            "--configuration",
            "Release",
            "--no-build",
            "--verbosity",
            "normal",
        ],
        None,
        &dotnet_env,
    ) {
        eprintln!("  {} dotnet test", console::style("✘").red().bold(),);
        print_failure_output(&message);
        return ExitCode::FAILURE;
    }
    eprintln!("  {} dotnet test", console::style("✔").green(),);

    ExitCode::SUCCESS
}

fn host_ffi_library_name() -> &'static str {
    if cfg!(target_os = "windows") {
        "webui_ffi.dll"
    } else if cfg!(target_os = "macos") {
        "libwebui_ffi.dylib"
    } else {
        "libwebui_ffi.so"
    }
}

fn print_failure_output(output: &str) {
    let separator = console::style("─".repeat(60)).dim();
    eprintln!("    {separator}");
    for line in output.lines().take(30) {
        eprintln!("    {line}");
    }
    let total = output.lines().count();
    if total > 30 {
        eprintln!(
            "    {} ({} more lines)",
            console::style("...").dim(),
            total - 30,
        );
    }
    eprintln!("    {separator}");
}

#[cfg(test)]
mod tests {
    use super::host_ffi_library_name;

    #[test]
    fn host_ffi_library_name_matches_platform() {
        let expected = if cfg!(target_os = "windows") {
            "webui_ffi.dll"
        } else if cfg!(target_os = "macos") {
            "libwebui_ffi.dylib"
        } else {
            "libwebui_ffi.so"
        };

        assert_eq!(host_ffi_library_name(), expected);
    }
}
