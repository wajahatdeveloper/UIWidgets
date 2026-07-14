# Asset Store / UPM publishing notes

Publisher: **AetherNexus**  
Package id: `com.aethernexus.uiwidgets`  
Min Unity: **6000.3.10f1**

## Dependencies

- `com.aethernexus.foundationplatform` only (UniTask ships **embedded inside FP** — do not add a UniTask package dependency)

## Pre-submit checklist

- [ ] Foundation Platform free UPM already live  
- [ ] `package.json` author AetherNexus; no `com.aethernexus.unitask` / `com.cysharp.unitask` dep  
- [ ] `LICENSE.md`, `Third-Party Notices.txt` present (point at FP for UniTask)  
- [ ] `Samples~/InputDialogDemo` imports cleanly  
- [ ] Upload via Publisher Portal UPM tools  

## Related

Paid GameEngineCore listing comes after free packages are live.
