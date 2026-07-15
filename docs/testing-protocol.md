# Testing Protocol

## Purpose

Use this protocol after changes to the plugin lifecycle, project, references,
deployment configuration, or public SimHub properties.

## Build verification

1. Confirm the working tree contains no unintended machine-specific files.
2. Build the solution with installed Visual Studio/MSBuild using the intended
   configuration.
3. Require a successful .NET Framework 4.8 build with no reference errors.
4. Confirm the expected DLL and PDB exist in `bin/Debug` or `bin/Release`.
5. Confirm the template-derived post-build deployment copied the DLL/PDB into
   the SimHub installation and the RESX file into `SimHub/Languages`.

Do not replace unresolved SimHub references, retarget the framework, or change
deployment paths merely to make a build pass.

## Manual SimHub health check

1. Fully exit SimHub before replacing a plugin DLL.
2. Build and allow the configured post-build deployment to complete.
3. Start SimHub.
4. Open **Settings > Plugins** and confirm the plugin is present and enabled.
5. Confirm its settings page opens without a load or XAML error.
6. Open SimHub's property picker and verify the expected public properties.
7. For the renamed bootstrap plugin, verify:
   - `FF1600Tires.PluginAlive` equals `1` while initialized.
   - `FF1600Tires.DebugText` equals
     `FF1600 tire estimator plugin running`.
8. Review SimHub logs for plugin initialization or shutdown exceptions.

Record the SimHub version, build configuration, commit, and observed result
when a manual verification establishes a new known-good baseline.

## Current verified baseline

The untouched `User.PluginSdkDemo` template has been built in Debug,
automatically deployed by its official post-build command, and manually
confirmed to load as **Demo plugin** in SimHub.
