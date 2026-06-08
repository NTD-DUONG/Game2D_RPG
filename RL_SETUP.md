# Hướng dẫn tích hợp NPC Reinforcement Learning

## 1. Cài ML-Agents trong Unity

Project đã được thêm dependency `com.unity.ml-agents` trong `Packages/manifest.json`.
Mở Unity để Package Manager tự resolve package.

## 2. Tạo scene huấn luyện

Tạo scene riêng, ví dụ `TrainingArena`, để tránh ảnh hưởng scene game chính.

Scene cần có:

- `ArenaManager`: GameObject rỗng gắn `TrainingArenaManager`.
- `Player`: Rigidbody2D, Collider2D, `TrainingHealth`, `TrainingPlayerBot`.
- `Enemy`: Rigidbody2D, Collider2D, `TrainingHealth`, `EnemyTrainingAgent`.
- `TrainingRangedAttack`: component trên Enemy, dùng prefab `Bullet` có sẵn để bắn Player.
- Boundary collider để giới hạn khu vực huấn luyện.

## 3. Cấu hình Enemy Agent

Trên Enemy cần thêm các component ML-Agents:

- `Behavior Parameters`
  - Behavior Name: `EnemyTraining`
  - Vector Observation Space Size: `6`
  - Actions: Discrete, 1 branch, branch size `6`
- `Decision Requester`
  - Decision Period: `5`
  - Take Actions Between Decisions: bật

Gán reference cho `EnemyTrainingAgent`:

- Player
- Player Health
- Enemy Health
- Enemy Rigidbody2D
- Ranged Attack
- Arena Manager

## 4. Cấu hình bắn đạn cho Enemy

Trên Enemy thêm component `TrainingRangedAttack`.

Gán các field:

- `Target`: kéo object `Player`.
- `Owner Health`: kéo `TrainingHealth` của Enemy.
- `Target Health`: kéo `TrainingHealth` của Player.
- `Bullet Prefab`: kéo `Assets/Prefabs/Weapons/Bullet.prefab`.
- `Fire Point`: có thể để trống, hoặc tạo child phía trước Enemy rồi kéo vào.
- `Bullet Move Speed`: `4`.
- `Projectile Range`: `10`.
- `Damage Amount`: `1`.

Trong `EnemyTrainingAgent`, field `Attack Hitbox` có thể để trống nếu dùng bắn đạn. Field `Ranged Attack` kéo component `TrainingRangedAttack` của Enemy vào.

## 5. Cấu hình Player training

Trong scene training, nên tắt `PlayerHealth` cũ và dùng `TrainingHealth` để reset episode sạch hơn.
Nếu muốn train tự động, thêm `TrainingPlayerBot` vào Player.

Nếu muốn tận dụng hitbox kiếm có sẵn của Player:

- Tìm object con `Weapon Collider` dưới Player.
- Add component `TrainingHitbox` vào `Weapon Collider`.
- `Owner`: kéo `TrainingHealth` của Player.
- Trong `TrainingPlayerBot`, field `Attack Hitbox`: kéo `Weapon Collider`.

Script `DamageSource` hiện cũng đã hỗ trợ gây sát thương lên `TrainingHealth`, nên kiếm cũ vẫn có thể đánh Enemy trong scene training.

## 6. Huấn luyện PPO

File cấu hình đã tạo tại `ml-agents-config/enemy_training_ppo.yaml`.

Lệnh train local mẫu:

C?i dependency Python trong venv:

```bash
python -m pip install --upgrade pip setuptools wheel
python -m pip install mlagents==1.1.0
python -m pip install --force-reinstall protobuf==3.20.3 onnx==1.15.0 torch==2.1.1
```

```bash
mlagents-learn ml-agents-config/enemy_training_ppo.yaml --run-id=enemy_rl_01
```

Sau khi terminal hiện thông báo Unity Environment đang chờ kết nối, mở scene `TrainingArena` trong Unity và bấm Play.

## 7. Sau khi train xong

ML-Agents sẽ tạo mô hình `.onnx` trong thư mục kết quả. Import file `.onnx` vào Unity, gán vào `Behavior Parameters` của Enemy và chuyển behavior sang inference để chạy NPC bằng mô hình đã huấn luyện.

## Ghi ch? v? Player trong l?c train

`TrainingArenaManager` c? 3 t?y ch?n b?t m?c ??nh ?? tr?nh Player ph? qu? tr?nh train:

- `Disable Player Input On Play`: t?t `PlayerController` khi v?o training.
- `Disable Player Weapon On Play`: t?t ki?m, `DamageSource` v? hitbox c?a Player.
- `Disable Player Bot On Play`: t?t `TrainingPlayerBot` n?u c?.

Nh? v?y Player ch? ??ng vai tr? m?c ti?u, Enemy RL c? th?i gian h?c di chuy?n v? b?n ??n. N?u mu?n t? test ?i?u khi?n Player, b? tick c?c t?y ch?n n?y tr?n `ArenaManager`.

Khi mu?n quan s?t game ? t?c ?? b?nh th??ng, ch?y train th?m tham s?:

```bash
mlagents-learn ml-agents-config/enemy_training_ppo.yaml --run-id=enemy_rl_debug --timeout-wait 600 --time-scale 1
```

## Ghi ch? l?m m??t chuy?n ??ng

C?c script training ?? t? b?t `Rigidbody2D Interpolation = Interpolate` v? d?ng gia t?c ?? ??i v?n t?c m??t h?n.

Trong `Decision Requester`, n?u NPC v?n ??i h??ng gi?t c?c khi quan s?t:

- ??t `Decision Period` t? `5` xu?ng `2` ho?c `1`.
- Gi? `Take Actions Between Decisions` b?t.
- Khi mu?n xem chuy?n ??ng ??ng t?c ?? game, ch?y train v?i `--time-scale 1`.

V? d?:

```bash
mlagents-learn ml-agents-config/enemy_training_ppo.yaml --run-id=enemy_rl_smooth --timeout-wait 600 --time-scale 1
```

## Ghi ch? v? hi?n t??ng teleport v? animation ch?y

N?u nh?n v?t th?nh tho?ng nh?y sang v? tr? kh?c, ?? l? reset episode/random spawn. ?? quan s?t ?? kh? ch?u:

- Trong `EnemyTrainingAgent`, gi? `Enforce Minimum Episode Time` b?t.
- ??t `Minimum Episode Time` kho?ng `120`.
- Khi debug b?ng m?t, ch?y trainer v?i `--time-scale 1`.

N?u Player ??ng y?n nh?ng v?n ch?y animation, `ArenaManager` ?? c? t?y ch?n `Force Player Idle Animation`. Gi? t?y ch?n n?y b?t khi train.

N?u Blue Slime ??ng y?n nh?ng animation v?n ch?y, `EnemyTrainingAgent` ?? c? `Pause Animator When Still`. Gi? t?y ch?n n?y b?t khi quan s?t.
