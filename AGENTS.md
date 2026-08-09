# EvolationSimulationV2 çalışma notları

- Aktif proje bu klasördeki V2 sürümüdür; V1 üzerinde değişiklik yapma.
- Kullanıcıya teslim edilen her değişiklik paketinde `ProjectSettings/ProjectSettings.asset` içindeki `bundleVersion` değerini bir patch kademe artır.
- Sürüm artarken `AndroidBundleVersionCode` değerini de bir artır.
- Oyun içindeki sürüm yazısı `Application.version` kullanmaya devam etmelidir.
