## Draft đồ án Version 1

Link Google Docs: [Draft Đồ án V1](https://docs.google.com/document/d/1xDEmi0crFSxgQDuGEasyYe1QRjx4Q0TTzJ_Zad5doS8/edit?usp=sharing)

# RL-Coop-Survival-Game

## Giới thiệu

Đồ án này là **Game sinh tồn Co-op dựa trên học tăng cường** sử dụng Unity và ML-Agents.  
Người chơi sẽ phối hợp trong một môi trường để **sinh tồn, thu lượm tài nguyên và chống lại kẻ địch (zombie, quái vật, robot...)**. Điểm nhấn của đồ án là **hệ thống AI thông minh**, có khả năng tự học, né tránh, phối hợp, và phản ứng theo hành vi người chơi.

Demo V1 tập trung vào việc **huấn luyện một ZombieAgent tự tìm đến Player trong môi trường đơn giản**, nhằm kiểm chứng cơ chế cơ bản của Reinforcement Learning.

---

## Cấu trúc repo

```
RL-Coop-Survival-Game/
├─ Assets/
│  ├─ Models/            # Chứa file .onnx sau khi huấn luyện
│  ├─ Scenes/            # Scene DemoV1
│  └─ Scripts/           # PlayerData.cs, EnemyData.cs, ZombieAgent.cs ...
├─ config/
│  └─ trainer_config.yaml # Cấu hình huấn luyện ML-Agents
├─ results/              # Thư mục lưu kết quả huấn luyện
└─ README.md
```

---

## Hướng dẫn chạy Demo V1

### 1. Yêu cầu

- Unity 2022 trở lên (khuyến nghị LTS)
- Python 3.9+
- Unity ML-Agents (v4.0)
- Các package Python cần thiết: `mlagents`, `tensorboard`

### 2. Cài đặt môi trường Python

```bash
python -m venv venv
venv\Scripts\activate       # Windows
pip install mlagents tensorboard
```

### 3. Chạy huấn luyện ZombieAgent

Trong thư mục gốc repo, chạy lệnh:

```bash
mlagents-learn ../config/trainer_config.yaml --run-id=demo_v1 --force
```

- Quan sát log: Mean Reward sẽ tăng dần.
- Sau khi huấn luyện (~200k bước), file `ZombieBehavior.onnx` sẽ được xuất ra trong `results/demo_v1/`.

### 4. Tích hợp mô hình vào Unity

- Copy `results/demo_v1/ZombieBehavior.onnx` vào `Assets/Models/`.
- Trong Unity, chọn Zombie → `Behavior Parameters` → kéo file `.onnx` vào.
- Chạy Scene `DemoV1`, Zombie sẽ tự di chuyển đến Player.

### 5. Theo dõi tiến trình học (tuỳ chọn)

```bash
tensorboard --logdir results --port 6006
```

- Mở trình duyệt: `http://localhost:6006` để xem biểu đồ Reward, Entropy, Episode Length.

---

## Định hướng phát triển

- Demo V2: thêm chướng ngại vật, cho Player di chuyển, tối ưu reward shaping.
- Mở rộng sang Co-op 2 người, phát triển AI phối hợp phức tạp.
