using UnityEngine;

namespace LibraryEscape
{
    /// <summary>
    /// Собирает второй уровень — банковское хранилище. План как на наброске:
    /// четыре денежные точки по углам, центральная вращающаяся камера с четырьмя
    /// лучами (без «мёртвых» секторов) и выход в правом верхнем углу.
    /// </summary>
    public class Level2Bootstrap : MonoBehaviour
    {
        [Header("Ассеты")]
        public LevelArt art;

        [Header("Настройки")]
        public int startingLives = 3;
        public float cameraSize = 7f;

        public const float RoomWidth = 20f;
        public const float RoomHeight = 16f;
        public static readonly Vector2 SpawnPoint = new Vector2(0f, -7f);
        public static readonly Vector2 CameraRigCenter = new Vector2(0f, 0.2f);

        private LevelArt _art;

        private void Start()
        {
            _art = art != null ? art : ScriptableObject.CreateInstance<LevelArt>();

            Transform geometry = new GameObject("Geometry").transform;
            geometry.SetParent(transform, false);

            BuildFloor(geometry);
            BuildWalls(geometry);
            BuildDecor(geometry);

            MoneyTable2D[] tables = BuildTables();
            SecurityCamera2D[] cameras = BuildCameraRig();
            VaultVent2D vent = BuildVent();

            Level2Game game = gameObject.AddComponent<Level2Game>();
            PlayerMover2D player = BuildPlayer();
            game.Initialize(player, cameras, tables, new HidingZone2D[0], vent, startingLives);

            SetupCamera(player.transform);
        }

        private void BuildFloor(Transform parent)
        {
            GameObject floorGo = new GameObject("Vault Floor");
            floorGo.transform.SetParent(parent, false);

            SpriteRenderer floor = floorGo.AddComponent<SpriteRenderer>();
            floor.sprite = VaultArt.Floor;
            floor.color = Color.white;
            floor.sortingOrder = -100;
            floor.drawMode = SpriteDrawMode.Tiled;
            floor.tileMode = SpriteTileMode.Continuous;
            floor.size = new Vector2(RoomWidth, RoomHeight);
        }

        private void BuildWalls(Transform parent)
        {
            Color wallColor = new Color(0.205f, 0.220f, 0.270f);
            Color trimColor = new Color(0.320f, 0.345f, 0.405f);

            // Внешние стены: блокируют и движение, и обзор камер.
            AddOuterWall(parent, "Wall Top", new Vector2(0f, 8.25f), new Vector2(RoomWidth + 0.5f, 0.5f),
                new Vector2(0f, -0.21f), new Vector2(RoomWidth + 0.5f, 0.08f), wallColor, trimColor);
            AddOuterWall(parent, "Wall Bottom", new Vector2(0f, -8.25f), new Vector2(RoomWidth + 0.5f, 0.5f),
                new Vector2(0f, 0.21f), new Vector2(RoomWidth + 0.5f, 0.08f), wallColor, trimColor);
            AddOuterWall(parent, "Wall Left", new Vector2(-10.25f, 0f), new Vector2(0.5f, RoomHeight + 0.5f),
                new Vector2(0.21f, 0f), new Vector2(0.08f, RoomHeight + 0.5f), wallColor, trimColor);
            AddOuterWall(parent, "Wall Right", new Vector2(10.25f, 0f), new Vector2(0.5f, RoomHeight + 0.5f),
                new Vector2(-0.21f, 0f), new Vector2(0.08f, RoomHeight + 0.5f), wallColor, trimColor);

            // Внутренние перегородки: только мешают ходить, камерам обзор не перекрывают.
            Color innerColor = new Color(0.245f, 0.260f, 0.315f);

            AddInnerWall(parent, "Stub Top Left", new Vector2(-4f, 6.75f), new Vector2(0.4f, 2.5f), innerColor);
            AddInnerWall(parent, "Stub Top Right", new Vector2(4f, 6.75f), new Vector2(0.4f, 2.5f), innerColor);
            AddInnerWall(parent, "Stub Bottom Left", new Vector2(-4f, -6.75f), new Vector2(0.4f, 2.5f), innerColor);
            AddInnerWall(parent, "Stub Bottom Right", new Vector2(4f, -6.75f), new Vector2(0.4f, 2.5f), innerColor);

            AddInnerWall(parent, "Shelf Left", new Vector2(-7f, 0.2f), new Vector2(6f, 0.4f), innerColor);
            AddInnerWall(parent, "Shelf Right", new Vector2(7f, 0.2f), new Vector2(6f, 0.4f), innerColor);

            // Стенка у входа: прикрывает точку старта от камеры. Выходы из ниши — слева и справа.
            AddInnerWall(parent, "Entrance Wall", new Vector2(0f, -5.7f), new Vector2(4f, 0.4f),
                innerColor);
        }

        private static void AddOuterWall(Transform parent, string name, Vector2 center, Vector2 size,
            Vector2 trimOffset, Vector2 trimSize, Color wallColor, Color trimColor)
        {
            PlaceholderArt.AddSprite(name, parent, PlaceholderArt.Square, wallColor, center, size, 4);
            PlaceholderArt.AddSprite(name + " Trim", parent, PlaceholderArt.Square, trimColor,
                center + trimOffset, trimSize, 5);

            AddWallCollider(parent, name, center, size, true);
        }

        private static void AddInnerWall(Transform parent, string name, Vector2 center, Vector2 size, Color color)
        {
            PlaceholderArt.AddSprite(name, parent, PlaceholderArt.Square, color, center, size, 4);
            // Внутренние стены тоже перекрывают обзор камерам — за ними можно спрятаться.
            AddWallCollider(parent, name, center, size, true);
        }

        private static void AddWallCollider(Transform parent, string name, Vector2 center, Vector2 size,
            bool blocksVision)
        {
            GameObject colliderGo = new GameObject(name + " Collider");
            colliderGo.transform.SetParent(parent, false);
            colliderGo.transform.localPosition = new Vector3(center.x, center.y, 0f);

            BoxCollider2D collider = colliderGo.AddComponent<BoxCollider2D>();
            collider.size = size;
            if (blocksVision) colliderGo.AddComponent<Obstacle2D>();
        }

        private void BuildDecor(Transform parent)
        {
            // Ящики в углах — просто антураж, обзор не перекрывают.
            Vector2[] crates =
            {
                new Vector2(-9.2f, 1.6f),
                new Vector2(9.2f, 1.6f),
                new Vector2(-9.2f, -1.6f),
                new Vector2(9.2f, -1.6f)
            };

            for (int i = 0; i < crates.Length; i++)
            {
                PlaceholderArt.AddSprite("Crate " + (i + 1), parent, VaultArt.Crate, Color.white,
                    crates[i], new Vector2(0.9f, 0.9f), 7);

                GameObject colliderGo = new GameObject("Crate " + (i + 1) + " Collider");
                colliderGo.transform.SetParent(parent, false);
                colliderGo.transform.localPosition = new Vector3(crates[i].x, crates[i].y, 0f);

                BoxCollider2D collider = colliderGo.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(0.85f, 0.85f);
            }
        }

        private MoneyTable2D[] BuildTables()
        {
            Vector2[] positions =
            {
                new Vector2(-7.2f, 5f),
                new Vector2(7.2f, 5f),
                new Vector2(-7.2f, -5f),
                new Vector2(7.2f, -5f)
            };

            MoneyTable2D[] tables = new MoneyTable2D[positions.Length];

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject root = new GameObject("Money Table " + (i + 1));
                root.transform.SetParent(transform, false);
                root.transform.position = new Vector3(positions[i].x, positions[i].y, 0f);

                MoneyTable2D table = root.AddComponent<MoneyTable2D>();
                table.Configure(null, 2);
                tables[i] = table;
            }

            return tables;
        }

        private HidingZone2D[] BuildHidingZones(Transform parent)
        {
            return new HidingZone2D[0];
        }

        /// <summary>Колонна в центре с двумя камерами: спереди и сзади, качаются влево-вправо.</summary>
        private SecurityCamera2D[] BuildCameraRig()
        {
            GameObject rig = new GameObject("Camera Column");
            rig.transform.SetParent(transform, false);
            rig.transform.position = new Vector3(CameraRigCenter.x, CameraRigCenter.y, 0f);

            // Колонна: светлый гранёный столб с тёмным сердечником.
            PlaceholderArt.AddSprite("Column", rig.transform, PlaceholderArt.Square,
                new Color(0.190f, 0.200f, 0.250f), Vector2.zero, new Vector2(2.2f, 2.2f), 6);
            PlaceholderArt.AddSprite("Column Face", rig.transform, PlaceholderArt.Square,
                new Color(0.270f, 0.290f, 0.350f), Vector2.zero, new Vector2(1.7f, 1.7f), 7);
            PlaceholderArt.AddSprite("Column Core", rig.transform, PlaceholderArt.Square,
                new Color(0.140f, 0.150f, 0.190f), Vector2.zero, new Vector2(0.7f, 0.7f), 8);

            // Колонна мешает ходить, но обзор камерам не перекрывает.
            BoxCollider2D columnCollider = rig.AddComponent<BoxCollider2D>();
            columnCollider.size = new Vector2(2.0f, 2.0f);

            // Две камеры: одна на передней грани, другая на задней.
            SecurityCamera2D front = CreateCamera(rig.transform, "Camera Front", Vector2.down, 0f, 9f);
            SecurityCamera2D back = CreateCamera(rig.transform, "Camera Back", Vector2.up, 1.7f, 11f);

            return new[] { front, back };
        }

        private static SecurityCamera2D CreateCamera(Transform parent, string name, Vector2 direction,
            float phase, float period)
        {
            GameObject cameraGo = new GameObject(name);
            cameraGo.transform.SetParent(parent, false);
            cameraGo.transform.localPosition = Vector3.zero;

            SecurityCamera2D camera = cameraGo.AddComponent<SecurityCamera2D>();
            // Амплитуда больше половины сектора: за цикл лучи перекрывают и бока,
            // поэтому постоянных «мёртвых» направлений не остаётся.
            camera.Configure(null, direction, 62f, period, phase, 11f, 55f, true, false, 0f, 0.95f);
            return camera;
        }

        private VaultVent2D BuildVent()
        {
            // Выход — в правом верхнем углу.
            GameObject root = new GameObject("Exit Vent");
            root.transform.SetParent(transform, false);
            root.transform.position = new Vector3(9.2f, 6.2f, 0f);

            SpriteRenderer grate = PlaceholderArt.AddSprite("Grate", root.transform, VaultArt.Grate,
                Color.white, new Vector2(0.68f, 0f), new Vector2(1.2f, 0.9f), 6);

            BoxCollider2D trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.4f, 1.8f);

            VaultVent2D vent = root.AddComponent<VaultVent2D>();
            vent.Configure(null, grate);

            return vent;
        }

        private PlayerMover2D BuildPlayer()
        {
            GameObject root = new GameObject("Player");
            root.transform.SetParent(transform, false);
            root.transform.position = new Vector3(SpawnPoint.x, SpawnPoint.y, 0f);

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

            Rigidbody2D rigidbody = root.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;
            rigidbody.freezeRotation = true;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.55f;

            PlayerMover2D mover = root.AddComponent<PlayerMover2D>();
            mover.moveSpeed = 5f;
            mover.facingPivot = pivotGo.transform;
            mover.animator = animator;
            return mover;
        }

        private void SetupCamera(Transform player)
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
            gameCamera.backgroundColor = new Color(0.045f, 0.050f, 0.065f);
            gameCamera.transform.position = new Vector3(0f, 0f, -10f);

            FollowCamera2D follow = gameCamera.GetComponent<FollowCamera2D>();
            if (follow == null) follow = gameCamera.gameObject.AddComponent<FollowCamera2D>();

            follow.orthographicSize = cameraSize;
            follow.SetTarget(player, new Rect(-RoomWidth * 0.5f, -RoomHeight * 0.5f, RoomWidth, RoomHeight));
        }
    }
}
