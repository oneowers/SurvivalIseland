# SurvivalIseland

## Sprint 2 - Tree Felling

### Добавлено
- Система рубки деревьев с накоплением ударов и выбором направления падения.
- `ChoppableTree`, `ChopInteraction`, `TreeFallSystem`, `TreeDecalSystem`, `LogPickup`.
- Умное обнаружение ближайшего дерева в поле зрения через `TreeTargetDetector` без Raycast для поиска целей.
- Конфиги `TreeConfig`, `TreeFallConfig`, `TreeTargetingConfig`.
- VContainer scope для модуля рубки деревьев: `TreeFellingLifetimeScope`.
- Адаптеры для инвентаря и carry anchor: `InventoryQueryAdapter`, `InventoryWriteAdapter`, `PlayerCarryAnchorAdapter`.
- Новые prefab-ресурсы для дерева, бревна, веток и decal.
- Debug-логи и gizmos для диагностики выбора дерева и цепочки взаимодействия.

### Изменено
- `PlayerInstaller` расширен для подключения `TreeTargetDetector` и `PlayerTreeInteractor`.
- `SampleScene` обновлена для Sprint 2: добавлены scope, конфиги, префабы и связи для рубки деревьев.
- Input и scene setup обновлены под взаимодействие `E` / `LMB` с деревьями.
- Настройка слоёв обновлена для отдельного слоя деревьев.

### Удалено
- Удалений игровых систем в Sprint 2 не производилось.
