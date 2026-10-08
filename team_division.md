# The Forgotten Village — Team Work Division

## Team Work Division

| Category | Jane | Jemmy |
|---|---|---|
| **Player System** | `PlayerController`, movement, Rigidbody, Collider | — |
| **Camera** | Camera follow, player-facing direction | — |
| **Interaction System** | `IInteractable`, press `E` interaction, door interaction, item pickup | — |
| **Inventory System** | Inventory, clue/item collection | — |
| **Dialogue System** | — | `DialogueManager`, NPC dialogue logic |
| **NPC System** | — | NPC behavior and dialogue trigger |
| **Quest System** | — | `QuestManager`, `QuestObjective`, quest progress |
| **UI System** | Interaction prompt | Dialogue UI, Quest UI |
| **House 1** | Elder's House | — |
| **House 2** | — | Alchemist's House |
| **House 3** | Carpenter's House | — |
| **House 4** | — | Hunter's House |
| **House 5** | Scholar's House | — |
| **House 6** | — | Abandoned / Final House |
| **Village Scene** | Shared, but only one person edits at a time | Shared, but only one person edits at a time |
| **GameManager** | Shared integration | Shared integration |
| **Story / Final Testing** | Shared | Shared |

---

## Branch Assignment

| A Partner Branches | B Partner Branches |
|---|---|
| `feature/player` | `feature/dialogue` |
| `feature/interaction` | `feature/quest` |
| `feature/inventory` | `feature/ui` |
| `feature/house-01-elder` | `feature/house-02-alchemist` |
| `feature/house-03-carpenter` | `feature/house-04-hunter` |
| `feature/house-05-scholar` | `feature/house-06-final` |

---

## Suggested Development Order

### Phase 1 — Core Systems

**A Partner**
- Player movement
- Camera
- Interaction system
- Item pickup
- Inventory

**B Partner**
- Dialogue system
- NPC system
- Quest system
- Dialogue UI
- Quest UI

### Phase 2 — Integration

Integrate the first complete gameplay loop:

```text
Player
  ↓
Interact with NPC
  ↓
Dialogue
  ↓
Quest Starts
  ↓
Enter House
  ↓
Complete Challenge
  ↓
Collect Clue
  ↓
Quest Updates
```

### Phase 3 — House Development

**A Partner**
- House 1 — Elder's House
- House 3 — Carpenter's House
- House 5 — Scholar's House

**B Partner**
- House 2 — Alchemist's House
- House 4 — Hunter's House
- House 6 — Abandoned / Final House

### Phase 4 — Final Integration

Both partners work together on:
- Village integration
- GameManager
- Story consistency
- UI polish
- Bug fixing
- Final testing
- Final build

---

## Collaboration Rules

1. Do not work directly on `main`.
2. Each feature should be developed in its own branch.
3. Use Pull Requests to merge completed features into `main`.
4. Do not use `git push --force` on `main`.
5. Do not edit the same `.unity` scene at the same time.
6. `Village.unity` should only be edited by one partner at a time.
7. Use Prefabs for shared objects such as Player, NPCs, Doors, and Items.
8. Always pull the latest `main` before starting a new feature branch.
9. Commit `.meta` files together with Unity assets.
10. Keep Unity `Asset Serialization` set to **Force Text** and `Version Control Mode` set to **Visible Meta Files**.
