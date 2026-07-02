# ADR-0009: Family Setup and Maintenance Wrapper

- Status: accepted
- Date: 2026-07-02
- Supersedes: none
- Related docs: [Production Installer Update Roadmap](../roadmaps/production-installer-update-roadmap.md), [Local Installation and Data Location](../operations/local-installation-and-data-location.md), [Upgrades Migrations and Recovery](../operations/upgrades-migrations-and-recovery.md)

## Context

Homeschool Manager uses Velopack for Windows application packaging and updates. Velopack's Windows `Setup.exe` is intentionally one-click and does not show custom install choices. Velopack uninstall hooks must exit quickly, cannot show UI, and cannot cancel uninstall. That means raw Velopack install/uninstall cannot provide the parent-facing choices Homeschool Manager needs for Always Available setup and family-record retention.

Families need a clear production experience:

- Always Available should be the default install choice.
- Open Only should remain available.
- Uninstall should tell the parent what happens to family records.
- Family records should be kept by default.
- Removing records should require explicit confirmation and should offer a safety archive first.

## Decision

Homeschool Manager will ship a Windows setup and maintenance wrapper named `HomeschoolManager-Family-Setup.exe` as the parent-facing installer and maintenance entry point.

The wrapper will:

- Run the Velopack package installer as the app/update engine.
- Present Always Available as the recommended default and Open Only as the fallback.
- Turn on Always Available by invoking the existing Windows background runner helper.
- Register itself as the per-user maintenance uninstall prompt after installation when the Velopack uninstall registry entry is available.
- Keep family records by default during uninstall.
- Require exact confirmation before removing family records.
- Create a safety archive before removing family records when the parent chooses that option.

Raw Velopack `Setup.exe` remains an advanced/package artifact. It should not be the file given to nontechnical families as the primary installer.

## Consequences

- Production release output includes both the raw Velopack package artifacts and the family setup wrapper.
- Windows Add/Remove can show the Homeschool Manager data-retention prompt when the app was installed through the family setup wrapper and the wrapper successfully registered the maintenance uninstall command.
- If a parent installs by running raw Velopack `Setup.exe` directly, Velopack uninstall behavior applies and will not show Homeschool Manager's data-retention prompt.
- The maintenance wrapper must not silently delete records.
- The maintenance wrapper must keep app binaries, updater packages, and family records separate.

## Guardrails

- Do not delete `%LOCALAPPDATA%/HomeschoolManagerData` or `%PROGRAMDATA%/HomeschoolManager` without explicit parent confirmation.
- Do not make raw Velopack `Setup.exe` the recommended family installer.
- Do not imply that Windows background access is active until the background runner is installed and running.
- Do not send family records to an external service as part of install, repair, update, or uninstall.
