# SurvivalIseland

## Sprint 2 - Tree Felling

Спринт 2 завершил внедрение системы рубки деревьев как отдельного игрового модуля. В проект был добавлен полный цикл взаимодействия: поиск дерева, нанесение ударов, накопление повреждений, падение ствола, спавн брёвен и визуальная обратная связь через decal-систему.

### Добавлено
- Модуль рубки деревьев с основными runtime-классами: `ChoppableTree`, `ChopInteraction`, `TreeFallSystem`, `TreeDecalSystem`, `LogPickup`, `PlayerTreeInteractor`.
- Умный поиск ближайшего дерева в поле зрения игрока через `TreeTargetDetector` с периодическим сканированием без постоянного `Raycast`.
- Конфиги `TreeConfig`, `TreeFallConfig`, `TreeTargetingConfig` для урона, падения, спавна брёвен, веток и параметров выбора цели.
- Отдельный `TreeFellingLifetimeScope` для регистрации зависимостей через `VContainer`.
- Пуллинг объектов через `ObjectPool<T>` для брёвен, веток и decal-элементов.
- Адаптеры инвентаря и carry-anchor: `InventoryQueryAdapter`, `InventoryWriteAdapter`, `PlayerCarryAnchorAdapter`.
- Новые игровые сообщения и события для ударов, падения дерева, звука и частиц.
- Префабы дерева, брёвен, веток и decal-ресурсы для сцены.

### Изменено
- `PlayerInstaller` расширен регистрацией `TreeTargetDetector` и `PlayerTreeInteractor`.
- Игрок получил полноценную цепочку интеракции с деревьями через существующий input flow.
- `SampleScene` обновлена под сценовый setup Sprint 2: добавлены префабы, конфиги, scope и runtime-ссылки.
- Слои и scene setup были подготовлены для отдельной обработки деревьев и взаимодействуемых объектов модуля.
- Инвентарь игрока был связан с системой подбора брёвен и переносом ресурсов после падения дерева.

### Удалено
- Отдельных игровых систем в рамках Sprint 2 удалено не было.
- Устаревший ручной сценарий тестирования рубки без DI и без сцепки с инвентарём больше не используется как целевой pipeline.

## Sprint 3 - Campfire System

Спринт 3 завершил внедрение трёхуровневой системы костра как центральной механики выживания. Костёр переведён в отдельный gameplay-модуль с runtime-state через `ScriptableObject`, асинхронным расходом топлива на `UniTask`, защитной зоной, управлением светом, точкой сохранения и расширенной интеграцией с инвентарём.

### Добавлено
- `CampfireState` как runtime `ScriptableObject` со статусом костра, текущим уровнем, топливом, радиусами защиты и света.
- `CampfireSystem` с асинхронным циклом сгорания топлива на `UniTask` без `Coroutine` и без `Update`.
- `CampfireProtectionZone` с проверкой safe-zone через `Physics.OverlapSphereNonAlloc` каждые `0.5` секунды.
- `CampfireLightController` для flicker-логики, dying-состояния и плавного затухания света после тушения.
- `CampfireSavePoint` с поддержкой сохранения, sleep-запроса и выбора костра как активной точки возрождения.
- `CampfireInteraction` для добавления топлива, длительного удержания взаимодействия и поджигания через источник огня.
- `CampfireLifetimeScope` для подключения модуля костра через `VContainer` и регистрации `MessagePipe`-событий.
- Новые события модуля: `FuelChangedEvent`, `CampfireLitEvent`, `CampfireDyingEvent`, `CampfireExtinguishedEvent`, `CampfireLevelUpEvent`, `PlayerInSafeZoneEvent`, `GhostInLightEvent`, `SleepRequestEvent`.
- Трёхуровневая конфигурация костра через `CampfireConfig` и `CampfireLevelData`.

### Изменено
- `ICampfireService` расширен до полноценного runtime-контракта с доступом к состоянию, уровням, радиусам, топливу, поджигу, тушению и улучшению костра.
- `CampfireConfig` переработан из упрощённого одноуровневого конфига в конфиг прогрессии с несколькими уровнями костра и погодными множителями расхода топлива.
- `InventoryQueryAdapter` расширен поддержкой источников огня: `Flint` и `Firesteel`.
- `InventoryWriteAdapter` расширен поддержкой расходования брёвен на пополнение топлива костра.
- Система призраков продолжает использовать `ICampfireService`, но теперь получает более точные runtime-данные по радиусу защиты и состоянию костра.
- Сценовый setup и данные проекта были подготовлены под добавление campfire-ассетов, конфигов и новых компонентов сцены.

### Удалено
- Упрощённая версия костра с фиксированным радиусом защиты, одной моделью топлива и `ITickable`-обновлением больше не используется как основная реализация.
- Старые поля конфига костра уровня `StartFuelSeconds`, `MaxFuelSeconds`, `FuelConsumptionPerSecond`, `MinLightIntensity` и `MaxLightIntensity` были заменены новой tier-based конфигурацией.
- Прежняя модель костра как только источника света без save-point и без interaction-слоя исключена из текущей архитектуры модуля.

## Sprint 4 - Day/Night Cycle And Survival Atmosphere

Спринт 4 завершил внедрение полного цикла суток с динамическим освещением, атмосферными переходами и событиями времени суток. День и ночь теперь работают как центральная мировая система: освещение, ambient, поведение костра, термальный урон и ночная активность призраков согласованы через `MessagePipe` и подключены через `VContainer`.

### Добавлено
- `DayNightSystem` с 20-минутным игровым днём, фазами `Dawn/Morning/Noon/Afternoon/Sunset/Dusk/Night/PreDawn`, игровыми тиками и поддержкой `SkipToMorning`.
- `SunLightController` для управления directional light по `AnimationCurve`: интенсивность, цветовая температура и поведение света на протяжении суток.
- `AmbientLightController` для управления ambient-освещением и переключения day/sunset/night профилей постобработки.
- `TemperatureSystem` с ночным холодом, дневным перегревом и лечением рядом с активным костром.
- `TimeOfDayEventsSystem` для публикации событий рассвета, заката, ночной активации призраков и предрассветного спавна особой угрозы.
- `DayNightConfig` как `ScriptableObject` с фазами дня и кривыми освещения.
- `HealthHudPresenter` и `HealthHudConfig` для игрового HUD здоровья с плавным заполнением, delayed damage bar и hit-flash.

### Изменено
- `ProjectBootstrapLifetimeScope` расширен регистрацией глобальных day/night, health и HUD сервисов.
- `CampfireLifetimeScope` интегрирован с day/night-модулем, температурой и системой призраков.
- `CampfireState`, `CampfireProtectionZone` и связанные runtime-сервисы доведены до стабильного старта сцены: костёр корректно публикует состояние топлива, safe-zone и стартует как активная точка защиты.
- `HealthSystem` расширен обработкой thermal damage и thermal heal без отдельной логики вне основного сервиса здоровья.
- `GhostSpawnSystem` переведён на фазовую логику ночи и предрассветья.
- `SampleScene` приведена к актуальному сценовому setup без временных debug-логов и с корректной связкой bootstrap, campfire и player runtime-точек.

### Связка С Рубкой Деревьев
- Sprint 2 c системой рубки деревьев остаётся частью основного gameplay-loop и продолжает работать в актуальной сцене вместе с костром и новым циклом суток.
- Сбор дерева, перенос брёвен и пополнение костра теперь естественно вписываются в ночной survival-ритм: дерево даёт топливо, костёр даёт свет и защиту, а day/night-система задаёт давление по времени суток.

### Удалено
- Старый `DayNightCycleSystem` и `DayNightCycleConfig` больше не используются.
- Временные debug-представления safe-zone, campfire runtime и служебный HUD-вывод удалены из финального состояния спринта.

## Sprint 6 - Ghost Enemies

В этом чате был собран полноценный модуль ночных призраков для survival-loop. Призраки переведены на отдельную архитектуру без `NavMesh`: движение работает через `CharacterController`, активация и деактивация идут от day/night-событий, свет анализируется через `Physics.OverlapSphereNonAlloc`, а спавн и возврат происходят через `ObjectPool<T>`.

### Добавлено
- Новый базовый класс `GhostBase` с pooled lifecycle, ночной активацией, движением сквозь геометрию и общей боевой логикой.
- Новый призрак `PaleDrift` со state machine `Wandering -> LightSeeking -> PlayerPursuing -> Retreating`.
- Новый призрак `LordWraith`, который появляется только в окне `03:00-04:00`, тянет игрока через `PlayerGravityPullEvent` и игнорирует слабую защиту костра.
- `GhostLightDetector` с дешёвым overlap-сканированием источников света без raycast.
- `GhostSpawnConfig` и обновлённый `GhostSpawnSystem` для роста числа призраков по дням, спавна из пула и очистки на рассвете.
- Новые prefab-ы `PaleDrift` и `LordWraith`, подключённые к сцене и конфигу спавна.

### Изменено
- `ProjectBootstrapLifetimeScope` расширен регистрацией ghost/runtime-сообщений и зависимостей для gravity pull.
- `PlayerMovementSystem` научен реагировать на `PlayerGravityPullEvent`.
- `CampfireLightController` реализует `ILightSource`, чтобы призраки могли реагировать на интенсивность света костра.
- `CampfireProtectionZone`, `TimeOfDayEventsSystem`, `SampleScene` и конфиги проекта обновлены под ночную активацию призраков и предрассветный спавн Lord Wraith.
- Старые `GhostPresenter` и `GhostSpawnerConfig` сохранены как совместимые обёртки над новой архитектурой.

### Доведено В Этом Чате
- Исправлена интеграция `VContainer`, чтобы player-scope видел global `MessagePipe`-регистрации для ghost gravity pull.
- Исправлена ошибка `A Character Controller cannot be a trigger`: trigger-коллайдеры призраков отделены от `CharacterController`.
- Исправлено поведение света: `PaleDrift` больше не считает костёр безопасной приманкой и корректно отступает за периметр освещения.
- Исправлен контактный урон: теперь призраки наносят урон не только через `OnTriggerEnter`, но и через надёжную proximity-проверку с кулдауном.
