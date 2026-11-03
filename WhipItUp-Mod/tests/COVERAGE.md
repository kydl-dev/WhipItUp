# WhipItUp - test coverage

**Automated** = covered by a test that ran green; **Manual** = needs a person; **Gap** = not covered. Never mark an untested behaviour as covered.

| Feature / requirement | Unit | Integration | TestLab | Manual | Status |
|---|---|---|---|---|---|
| Loads with exact KydlCore, no errors | - | ManifestTests, ArchitectureRulesTests | selftest | - | Automated |
| 21 weapons exist with recipes and stations | WeaponCatalog | - | all-21-weapons, recipes-stations | - | Automated |
| Damage ladder / flat metal cost | Ladder, Functions | - | damage-ladder, flat-metal-cost | - | Automated |
| Whips skill | - | - | skills | - | Automated |
| Give, equip, craft a paddle | - | - | give-and-equip, craft-a-paddle | - | Automated |
| KY hub tabs, feature switches live | - | - | hub-page, feature-toggle | hub-look | Automated + Manual |
| Settings layout, patch targets, suite rules | - | SettingsTests, PatchTargetsTests, SuiteRulesTests | - | - | Automated |
| Lash frames data | WhipLashFramesTests | ManifestTests | - | - | Automated |
| Whip combo animations, coil, grab and pull, hit-stop | HitStopMath | - | - | in-game test | Gap |
| Sounds once per swing | - | - | - | in-game test | Gap |
| Item / armor stand display | - | - | - | in-game test | Gap |
| Multiplayer (EveryoneMustHaveMod, virtual peer) | - | - | - | 2-player test | Gap |