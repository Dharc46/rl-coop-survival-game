Demo V1 Scene Setup (quick steps)
1. New Scene:
   - File -> New Scene. Save as DemoV1.unity

2. Environment:
   - Create Plane: GameObject -> 3D Object -> Plane. Name = "Floor".
   - Create 4 walls: GameObject -> 3D Object -> Cube. Scale and position to make 10x10 room.
   - Optional: create empty GameObject "Room" and parent walls + floor.

3. Player (Target):
   - GameObject -> 3D Object -> Cube. Name = "Player".
   - Position: (0, 0.5, 0). Add BoxCollider (default).
   - (Optional) Tag as "Player".

4. Zombie (Agent):
   - GameObject -> 3D Object -> Capsule. Name = "Zombie".
   - Add Collider (CapsuleCollider) and optionally Rigidbody (isKinematic true if using transform movement).
   - Attach script: Assets/Scripts/Agents/ZombieAgent.cs
   - In Inspector: assign Player Transform to playerTransform field.
   - Assign an EnemyData ScriptableObject to enemyData (create one: Right-click in Project -> Create -> DemoV1 -> Data -> EnemyData).

5. ML-Agents:
   - Install ML-Agents package via Package Manager or following docs.
   - On Zombie GameObject, add "Behavior Parameters" component:
       - Behavior Name: "ZombieBehavior"
       - Vector Observation: space size = 4
       - Action Type: Discrete -> Branches = 1 -> Branch 0 Size = 4
   - Add Decision Requester if you want automatic decisions.

6. Test in Editor:
   - Add Heuristic control by pressing Play and using WASD (Zombie uses Heuristic).
   - Verify when Zombie approaches Player (within successDistance), episode ends.

7. Training:
   - Run: mlagents-learn config/trainer_config.yaml --run-id=demo_v1 --force
   - In Unity Editor, press Play. Training should begin.
