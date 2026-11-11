using UnityEngine;

namespace LibraryEscape
{
    /// <summary>
    /// Собирает уровень 1 целиком в рантайме: комнату-библиотеку по нарисованному плану,
    /// игрока, охранника, комоды с зонами статуса, дверь, камеру и правила игры.
    /// </summary>
    public class Level1Bootstrap : MonoBehaviour
    {
        [Header("Ассеты")]
        [Tooltip("Сюда можно положить ассет LevelArt с нарисованными спрайтами.")]
        public LevelArt art;

        [Header("Настройки")]
        public float playerSpeed = 5f;
        public float guardSpeed = 2.925f;
        public float guardViewDistance = 7f;
        public float guardViewAngle = 80f;
        public int startingLives = 3;
        public float roundSeconds = 60f;
        public float cameraSize = 6f;

        public const float RoomWidth = 25f;
        public const float RoomHeight = 50f;
        public static readonly Vector2 SpawnPoint = new Vector2(0f, -23.4f);

        private const float DoorCenterX = -1.05f;
        private const float DoorFrontY = 22f;

        /// <summary>
        /// Маршрут патруля по свободным проходам. Охранник идёт вниз по центральному
        /// проходу, обходит верхний стеллаж слева, возвращается в центр, заглядывает
        /// вниз в нижний зал, поднимается правым проходом и возвращается по верху.
        /// Точки подобраны так, чтобы не проходить сквозь стеллажи и колонны.
        /// </summary>
        private static readonly Vector2[] GuardRoute =
        {
            new Vector2(0f, 12f),
            new Vector2(0f, -2f),
            new Vector2(-9.5f, -2f),
            new Vector2(-9.5f, -7.3f),
            new Vector2(0f, -7.3f),
            new Vector2(0f, -13.5f),
            new Vector2(0f, -7.3f),
            new Vector2(9.5f, -7.3f),
            new Vector2(9.5f, 3f),
            new Vector2(9.5f, 18f),
            new Vector2(0f, 18f)
        };

        private LevelArt _art;

        private void Start()
        {
            _art = art != null ? art : ScriptableObject.CreateInstance<LevelArt>();

            Transform geometry = new GameObject("Geometry").transform;
            geometry.SetParent(transform, false);

            BuildFloor(geometry);
            RoomView2D roomView = BuildRoomBackground(geometry);
            BuildColliders(geometry);

            PlayerMover2D player = BuildPlayer();
            GuardPatrol2D guard = BuildGuard(player.transform);
            Commode2D[] commodes = BuildCommodes();
            SecretDoor door = BuildDoor(roomView);

            Camera gameCamera = SetupCamera();
            Level1Game game = gameObject.AddComponent<Level1Game>();
            game.Initialize(_art, player, guard, commodes, door, startingLives, roundSeconds);

            if (gameCamera != null)
            {
                FollowCamera2D follow = gameCamera.GetComponent<FollowCamera2D>();
                if (follow != null)
                {
                    follow.orthographicSize = cameraSize;
                    follow.SetTarget(player.transform,
                        new Rect(-RoomWidth * 0.5f, -RoomHeight * 0.5f, RoomWidth, RoomHeight));
                }
            }
        }

        private void BuildFloor(Transform parent)
        {
            Transform floorRoot = new GameObject("Floor").transform;
            floorRoot.SetParent(parent, false);

            GameObject floorGo = new GameObject("Floor Tiles");
            floorGo.transform.SetParent(floorRoot, false);

            SpriteRenderer floor = floorGo.AddComponent<SpriteRenderer>();
            floor.sprite = _art.FloorSprite;
            floor.color = Color.white;
            floor.sortingOrder = -100;
            floor.drawMode = SpriteDrawMode.Tiled;
            floor.tileMode = SpriteTileMode.Continuous;
            floor.size = new Vector2(RoomWidth, RoomHeight);
        }

        private RoomView2D BuildRoomBackground(Transform parent)
        {
            GameObject roomGo = new GameObject("Room Plan");
            roomGo.transform.SetParent(parent, false);

            SpriteRenderer renderer = roomGo.AddComponent<SpriteRenderer>();
            renderer.sprite = _art.roomClosed != null ? _art.roomClosed : _art.roomOpened;
            renderer.color = Color.white;
            renderer.sortingOrder = -90;

            RoomView2D view = roomGo.AddComponent<RoomView2D>();
            view.Configure(renderer, _art.roomClosed, _art.roomOpened);
            return view;
        }

        /// <summary>Невидимые коллайдеры стен и нарисованной мебели.</summary>
        private void BuildColliders(Transform parent)
        {
            Transform root = new GameObject("Colliders").transform;
            root.SetParent(parent, false);

            // Границы комнаты.
            AddCollider(root, "Wall Bottom", new Vector2(0f, -25.15f), new Vector2(25.4f, 0.5f));
            AddCollider(root, "Wall Left", new Vector2(-12.65f, 0f), new Vector2(0.5f, 50.4f));
            AddCollider(root, "Wall Right", new Vector2(12.65f, 0f), new Vector2(0.5f, 50.4f));

            // Верхняя кирпичная стена с проёмом двери (-2.05..-0.05).
            AddCollider(root, "Wall Top Left", new Vector2(-7.4f, 23.775f), new Vector2(10.7f, 2.45f));
            AddCollider(root, "Wall Top Right", new Vector2(6.35f, 23.775f), new Vector2(12.8f, 2.45f));

            // Стол в левом верхнем углу.
            AddCollider(root, "Desk", new Vector2(-7.675f, 21.3f), new Vector2(4.65f, 2.1f));

            // Вертикальные стеллажи-колонны.
            AddCollider(root, "Pillar Left", new Vector2(-2.175f, 7.75f), new Vector2(0.85f, 17f));
            AddCollider(root, "Pillar Middle", new Vector2(4.875f, 7.75f), new Vector2(0.85f, 17f));
            AddCollider(root, "Pillar Right", new Vector2(12.025f, 7.75f), new Vector2(0.85f, 17f));

            // Горизонтальные ряды стеллажей.
            AddCollider(root, "Shelf Row Top", new Vector2(0.075f, -4.65f), new Vector2(14.95f, 2.9f));
            AddCollider(root, "Shelf Row 2 Left", new Vector2(-7.525f, -9.9f), new Vector2(9.95f, 2.9f));
            AddCollider(root, "Shelf Row 2 Right", new Vector2(7.475f, -9.9f), new Vector2(9.95f, 2.9f));
            AddCollider(root, "Shelf Row 3 Left", new Vector2(-5.075f, -15.6f), new Vector2(4.95f, 2.9f));
            AddCollider(root, "Shelf Row 3 Right", new Vector2(5.01f, -15.6f), new Vector2(5.03f, 2.9f));
            AddCollider(root, "Shelf Row 4 Left", new Vector2(-7.525f, -20.8f), new Vector2(9.95f, 2.9f));
            AddCollider(root, "Shelf Row 4 Right", new Vector2(7.475f, -20.8f), new Vector2(9.95f, 2.9f));
        }

        private static void AddCollider(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(position.x, position.y, 0f);

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = size;
            go.AddComponent<Obstacle2D>();
        }

        private PlayerMover2D BuildPlayer()
        {
            GameObject root = new GameObject("Player");
            root.transform.SetParent(transform, false);
            root.transform.position = new Vector3(SpawnPoint.x, SpawnPoint.y, 0f);

            // Тело вращается вместе с пивотом, чтобы персонаж смотрел по направлению движения.
            // Спрайт нарисован лицом вверх, поэтому стартовое «вниз» — это поворот на 180°.
            GameObject pivotGo = new GameObject("Facing Pivot");
            pivotGo.transform.SetParent(root.transform, false);
            pivotGo.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);

            GameObject bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(pivotGo.transform, false);
            SpriteRenderer body = bodyGo.AddComponent<SpriteRenderer>();
            body.sprite = _art.PlayerSprite;
            body.color = _art.player != null ? Color.white : _art.playerColor;
            body.sortingOrder = 10;
            PlaceholderArt.FitToSize(bodyGo.transform, body.sprite, new Vector2(1.5f, 1.5f));

            SpriteFrameAnimator animator = bodyGo.AddComponent<SpriteFrameAnimator>();
            animator.target = body;
            animator.SetFrames(_art.PlayerWalkFrames, 9f);
            animator.playing = false;

            GameObject shieldGo = new GameObject("Shield");
            shieldGo.transform.SetParent(root.transform, false);
            SpriteRenderer shield = shieldGo.AddComponent<SpriteRenderer>();
            shield.sprite = _art.ShieldSprite;
            shield.color = _art.shieldColor;
            shield.sortingOrder = 12;
            PlaceholderArt.FitToSize(shieldGo.transform, shield.sprite, new Vector2(2.1f, 2.1f));
            shield.enabled = false;

            Rigidbody2D rigidbody = root.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;
            rigidbody.freezeRotation = true;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.55f;

            PlayerMover2D mover = root.AddComponent<PlayerMover2D>();
            mover.moveSpeed = playerSpeed;
            mover.facingPivot = pivotGo.transform;
            mover.shieldRenderer = shield;
            mover.animator = animator;
            return mover;
        }

        private GuardPatrol2D BuildGuard(Transform player)
        {
            // Охранник стартует в центральном проходе и патрулирует зал по маршруту.
            GameObject root = new GameObject("Guard");
            root.transform.SetParent(transform, false);
            root.transform.position = new Vector3(GuardRoute[0].x, GuardRoute[0].y, 0f);

            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.7f;

            GuardPatrol2D guard = root.AddComponent<GuardPatrol2D>();
            guard.moveSpeed = guardSpeed;
            guard.viewDistance = guardViewDistance;
            guard.viewAngle = guardViewAngle;
            guard.startFacing = Vector2.down;
            guard.Configure(_art, GuardRoute, player, null);
            return guard;
        }

        private Commode2D[] BuildCommodes()
        {
            // Комоды нарисованы двусторонними: одна модель = два комода (лицо и тыл).
            // Поэтому четыре нарисованные модели дают восемь комодов, у каждого своя
            // зелёная зона и своё состояние замка.
            Vector2[] positions =
            {
                new Vector2(-11.225f, 11.7f),
                new Vector2(-11.225f, 3.9f),
                new Vector2(-8.875f, -15.6f),
                new Vector2(8.8f, -15.6f)
            };
            const int sidesPerModel = 2;

            int total = positions.Length * sidesPerModel;
            int[] digits = BuildRandomCode(5);
            Commode2D[] commodes = new Commode2D[total];

            // Перемешиваем стороны (Fisher–Yates), чтобы листки каждый раз лежали в разных комодах.
            int[] order = new int[total];
            for (int i = 0; i < order.Length; i++) order[i] = i;

            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int swap = order[i];
                order[i] = order[j];
                order[j] = swap;
            }

            // Первые digits.Length сторон из перемешанного списка получают листки.
            int[] ordinals = new int[total];
            for (int i = 0; i < digits.Length && i < order.Length; i++)
            {
                ordinals[order[i]] = i + 1;
            }

            int index = 0;
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject root = new GameObject("Commode " + (i + 1));
                root.transform.SetParent(transform, false);
                root.transform.position = new Vector3(positions[i].x, positions[i].y, 0f);

                for (int s = 0; s < sidesPerModel; s++)
                {
                    bool hasPaper = ordinals[index] > 0;
                    int ordinal = hasPaper ? ordinals[index] : 0;
                    int digit = hasPaper ? digits[ordinal - 1] : 0;

                    GameObject go = new GameObject(s == 0 ? "Front" : "Back");
                    go.transform.SetParent(root.transform, false);
                    go.transform.localPosition = Vector3.zero;

                    Commode2D commode = go.AddComponent<Commode2D>();
                    commode.Configure(_art, ordinal, digit, hasPaper, null, s, s == 0);
                    commodes[index] = commode;
                    index++;
                }
            }

            return commodes;
        }

        /// <summary>Случайный код: разные цифры от 0 до 9 в случайном порядке.</summary>
        private static int[] BuildRandomCode(int length)
        {
            int[] pool = new int[10];
            for (int i = 0; i < pool.Length; i++) pool[i] = i;

            for (int i = pool.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int swap = pool[i];
                pool[i] = pool[j];
                pool[j] = swap;
            }

            int count = Mathf.Clamp(length, 1, pool.Length);
            int[] code = new int[count];
            for (int i = 0; i < count; i++) code[i] = pool[i];
            return code;
        }

        private SecretDoor BuildDoor(RoomView2D roomView)
        {
            GameObject root = new GameObject("Secret Door");
            root.transform.SetParent(transform, false);
            root.transform.position = new Vector3(DoorCenterX, DoorFrontY, 0f);

            BoxCollider2D trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.8f, 0.8f);

            SecretDoor door = root.AddComponent<SecretDoor>();
            door.roomView = roomView;

            // Твёрдый шкаф в проёме: исчезает после верного кода.
            GameObject blockerGo = new GameObject("Cabinet Blocker");
            blockerGo.transform.SetParent(root.transform, false);
            blockerGo.transform.localPosition = new Vector3(0f, 1.775f, 0f);
            BoxCollider2D blocker = blockerGo.AddComponent<BoxCollider2D>();
            blocker.size = new Vector2(2f, 2.45f);
            blockerGo.AddComponent<Obstacle2D>();
            door.cabinetBlocker = blocker;

            // Триггер победы в самом проёме — срабатывает только после открытия.
            GameObject exitGo = new GameObject("Exit Trigger");
            exitGo.transform.SetParent(root.transform, false);
            exitGo.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            BoxCollider2D exit = exitGo.AddComponent<BoxCollider2D>();
            exit.isTrigger = true;
            exit.size = new Vector2(1.6f, 1.4f);
            DoorExit2D exitTrigger = exitGo.AddComponent<DoorExit2D>();
            exitTrigger.door = door;

            return door;
        }

        private Camera SetupCamera()
        {
            Camera gameCamera = Camera.main;
            if (gameCamera == null)
            {
                GameObject cameraGo = new GameObject("Main Camera");
                cameraGo.tag = "MainCamera";
                gameCamera = cameraGo.AddComponent<Camera>();
                cameraGo.AddComponent<AudioListener>();
            }

            gameCamera.orthographic = true;
            gameCamera.orthographicSize = cameraSize;
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = _art.backgroundColor;
            gameCamera.transform.position = new Vector3(SpawnPoint.x, SpawnPoint.y + 3f, -10f);

            FollowCamera2D follow = gameCamera.GetComponent<FollowCamera2D>();
            if (follow == null) follow = gameCamera.gameObject.AddComponent<FollowCamera2D>();
            follow.orthographicSize = cameraSize;

            return gameCamera;
        }
    }
}
