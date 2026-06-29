using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

public enum ActionMode { None, Move, Attack, Special }

public class GameManager : MonoBehaviour
{
    public int amountOfPlayers;
    int currentPlayer;

    public int actionsPerTurn;
    int currentTurnActions;

    Creature selectedCreature;
    private ActionMode currentMode = ActionMode.None;
    private CreatureMove pendingMove;

    public TextMeshProUGUI turnText;
    public ActionPointsUI actionPointsUI;
    public Material[] playerSkyboxes;
    public GameObject victoryScreen;
    public TextMeshProUGUI victoryText;
    public int deathPoints = 0;
    public TextMeshProUGUI deathPointsText;
    public Button evolveButton; // legacy standalone button - safe to leave unassigned if ActionUI replaces it
    private bool battleStarted = false;
    private bool isCapturing = false;
    public void SetCapturing(bool value) => isCapturing = value;
    public Material[] playerMaterials;

    [Header("Evolution Choice")]
    [Tooltip("The EvolutionChoiceUI panel in the scene.")]
    public EvolutionChoiceUI evolutionChoiceUI;

    private void Awake()
    {
        BlackBoard.gameManager = this;
    }

    public Creature GetSelectedCreature()
    {
        return selectedCreature;
    }

    private void Start()
    {
        StartTurn();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
            Deselect();
    }

    public void StartBattle()
    {
        battleStarted = true;
        deathPoints = 0;
        UpdateDeathPointsUI();
        currentPlayer = 0;
        StartTurn();
    }

    void StartTurn()
    {
        currentTurnActions = actionsPerTurn;
        selectedCreature = null;
        currentMode = ActionMode.None;
        pendingMove = null;
        actionPointsUI.SetupCircles(actionsPerTurn);
        actionPointsUI.UpdateCircles(currentTurnActions, actionsPerTurn);
        if (playerSkyboxes != null && playerSkyboxes.Length > currentPlayer)
            RenderSettings.skybox = playerSkyboxes[currentPlayer];

        // Defensive Stance lasts "until the player's next turn" - end it now that their turn has come around again
        Creature[] allCreatures = FindObjectsByType<Creature>(FindObjectsSortMode.None);
        foreach (Creature c in allCreatures)
        {
            if (c.assignedPlayer == currentPlayer && c.inDefensiveStance)
                c.ExitDefensiveStance();
        }

        if (ActionUI.Instance != null)
            ActionUI.Instance.Hide();

        UpdateTurnUI();
    }

    public void EndTurn()
    {
        currentPlayer++;
        if (currentPlayer >= amountOfPlayers)
            currentPlayer = 0;
        StartTurn();

        if (currentPlayer == 1)
            StartCoroutine(SimpleAI.Instance.TakeTurn(actionsPerTurn));
    }

    void UpdateTurnUI()
    {
        turnText.text = currentPlayer == 0 ? "Your Turn" : "Opponent's Turn";
    }

    void SpendAction(int actionCost)
    {
        currentTurnActions -= actionCost;
        actionPointsUI.UpdateCircles(currentTurnActions, actionsPerTurn);
    }

    bool HasActionsLeft()
    {
        return currentTurnActions > 0;
    }

    // ── Tile clicks ──────────────────────────────────────────────────────
    public void ClickOnTile(Tile _clicked)
    {
        if (isCapturing) return;
        if (PlacementManager.Instance.IsPlacing)
        {
            PlacementManager.Instance.HandleTileClick(_clicked);
            return;
        }

        if (selectedCreature == null)
        {
            TrySelect(_clicked);
            return;
        }

        if (_clicked == selectedCreature.currentTile)
        {
            Deselect();
            return;
        }

        if (currentMode == ActionMode.None
            && _clicked.currentCreatureOnTile != null
            && !_clicked.currentCreatureOnTile.dead
            && _clicked.currentCreatureOnTile.assignedPlayer == currentPlayer)
        {
            SelectCreature(_clicked.currentCreatureOnTile);
            return;
        }

        if (!HasActionsLeft()) { Deselect(); return; }

        switch (currentMode)
        {
            case ActionMode.Move:
                HandleMoveClick(_clicked);
                break;
            case ActionMode.Attack:
                HandleAttackClick(_clicked);
                break;
            case ActionMode.Special:
                HandleSpecialClick(_clicked);
                break;
        }
    }

    void TrySelect(Tile _clicked)
    {
        if (_clicked.currentCreatureOnTile != null
            && !_clicked.currentCreatureOnTile.dead
            && _clicked.currentCreatureOnTile.assignedPlayer == currentPlayer)
        {
            SelectCreature(_clicked.currentCreatureOnTile);
        }
        else
        {
            Deselect();
        }
    }

    void SelectCreature(Creature c)
    {
        if (Tile.selectedTile != null)
            Tile.selectedTile.SetMaterial(Tile.selectedTile.originalMaterial);

        selectedCreature = c;
        currentMode = ActionMode.None;
        pendingMove = null;

        Tile.selectedTile = c.currentTile;
        Tile.selectedTile.SetMaterial(Tile.selectedTile.SelectedMaterial);

        if (ActionUI.Instance != null)
            ActionUI.Instance.Show(c);
        RefreshHighlights();
    }

    void HandleMoveClick(Tile clicked)
    {
        List<Tile> validTiles = BlackBoard.gridManager.GetTilesInRange(
            selectedCreature.currentTile, selectedCreature.moveMinRange, selectedCreature.moveRange);

        if (validTiles.Contains(clicked) && clicked.currentCreatureOnTile == null && clicked.currentObstacle == null)
            StartCoroutine(ExecuteMove(clicked));
        else
            CancelAction();
    }

    void HandleAttackClick(Tile clicked)
    {
        List<Tile> validTiles = BlackBoard.gridManager.GetTilesInRange(
            selectedCreature.currentTile, selectedCreature.attackMinRange, selectedCreature.attackRange);

        if (!validTiles.Contains(clicked)) { CancelAction(); return; }

        if (clicked.currentObstacle != null)
            StartCoroutine(ExecuteAttackObstacle(clicked.currentObstacle));
        else if (clicked.currentCreatureOnTile != null)
            StartCoroutine(ExecuteAttack(clicked.currentCreatureOnTile));
        else
            CancelAction();
    }

    void HandleSpecialClick(Tile clicked)
    {
        if (pendingMove == null) { CancelAction(); return; }

        List<Tile> validTiles = pendingMove.GetValidTargetTiles(selectedCreature);
        if (validTiles.Contains(clicked))
            StartCoroutine(ExecuteSpecial(pendingMove, clicked, clicked.currentCreatureOnTile));
        else
            CancelAction();
    }

    // ── Called by ActionUI buttons ───────────────────────────────────────
    public void SelectActionMode(ActionMode mode)
    {
        if (selectedCreature == null || !HasActionsLeft()) return;
        currentMode = mode;
        pendingMove = null;
        RefreshHighlights();
    }

    public void SelectSpecialMove(CreatureMove move)
    {
        if (selectedCreature == null || !HasActionsLeft() || move == null) return;

        currentMode = ActionMode.Special;
        pendingMove = move;
        RefreshHighlights();

        if (!move.requiresTarget)
            StartCoroutine(ExecuteSpecial(move, null, null));
    }

    void CancelAction()
    {
        currentMode = ActionMode.None;
        pendingMove = null;
        RefreshHighlights();
    }

    // ── Coroutine action execution (animation hook, then the actual effect) ──
    private IEnumerator ExecuteMove(Tile destination)
    {
        Creature mover = selectedCreature;
        yield return StartCoroutine(mover.PlayMoveAnimation());
        mover.Moveto(destination.transform.position, destination);
        SpendAction(mover.moveActionCost);
        FinishAction();
    }

    private IEnumerator ExecuteAttack(Creature target)
    {
        Creature attacker = selectedCreature;
        yield return StartCoroutine(attacker.PlayAttackAnimation(target));
        attacker.Attack(target);
        SpendAction(attacker.attackActionCost);
        FinishAction();
    }

    private IEnumerator ExecuteAttackObstacle(Obstacle obstacle)
    {
        Creature attacker = selectedCreature;
        yield return StartCoroutine(attacker.PlayAttackAnimation(null));
        obstacle.TakeDamage(attacker.attackDamage);
        SpendAction(attacker.attackActionCost);
        FinishAction();
    }

    private IEnumerator ExecuteSpecial(CreatureMove move, Tile targetTile, Creature targetCreature)
    {
        Creature user = selectedCreature;
        yield return StartCoroutine(move.PlayAnimation(user, targetTile, targetCreature));
        move.Execute(user, targetTile, targetCreature);
        SpendAction(move.actionCost);
        FinishAction();
    }

    void FinishAction()
    {
        Deselect();
    }

    public void RefreshHighlights()
    {
        BlackBoard.gridManager.ResetGridHighlights();
        if (selectedCreature != null)
        {
            switch (currentMode)
            {
                case ActionMode.None:
                    BlackBoard.gridManager.HighlightMoveRange(selectedCreature.currentTile, selectedCreature.moveMinRange, selectedCreature.moveRange);
                    BlackBoard.gridManager.HighlightAttackRange(selectedCreature.currentTile, selectedCreature.moveMinRange, selectedCreature.moveRange, selectedCreature.attackMinRange, selectedCreature.attackRange);
                    break;
                case ActionMode.Move:
                    BlackBoard.gridManager.HighlightMoveRange(selectedCreature.currentTile, selectedCreature.moveMinRange, selectedCreature.moveRange);
                    break;
                case ActionMode.Attack:
                    foreach (Tile t in BlackBoard.gridManager.GetTilesInRange(selectedCreature.currentTile, selectedCreature.attackMinRange, selectedCreature.attackRange))
                        t.SetMaterial(t.AttackRangeMaterial);
                    break;
                case ActionMode.Special:
                    if (pendingMove != null)
                        foreach (Tile t in pendingMove.GetValidTargetTiles(selectedCreature))
                            t.SetMaterial(BlackBoard.gridManager.moveRangeMaterial);
                    break;
            }
        }
        UpdateDeathPointsUI();
    }

    void Deselect()
    {
        selectedCreature = null;
        currentMode = ActionMode.None;
        pendingMove = null;
        BlackBoard.gridManager.ResetGridHighlights();
        if (Tile.selectedTile != null)
        {
            Tile.selectedTile.SetMaterial(Tile.selectedTile.originalMaterial);
            Tile.selectedTile = null;
        }
        if (ActionUI.Instance != null)
            ActionUI.Instance.Hide();
        UpdateDeathPointsUI();
    }

    public void OnPlayerCreatureDied()
    {
        deathPoints++;
        UpdateDeathPointsUI();
    }

    void UpdateDeathPointsUI()
    {
        if (deathPointsText != null)
            deathPointsText.text = "Evolutions available: " + deathPoints;

        if (evolveButton != null)
        {
            bool canEvolve = deathPoints > 0
                && selectedCreature != null
                && !selectedCreature.isEvolved
                && (selectedCreature.evolvedFormPrefab != null
                    || selectedCreature.evolvedFormPrefabB != null);

            evolveButton.interactable = canEvolve;
        }
    }

    // ── Evolution ─────────────────────────────────────────────────────────
    public void TryEvolveSelected()
    {
        if (selectedCreature == null) return;
        if (selectedCreature.isEvolved) return;
        if (deathPoints <= 0) return;
        if (!HasActionsLeft()) return;

        bool hasA = selectedCreature.evolvedFormPrefab != null;
        bool hasB = selectedCreature.evolvedFormPrefabB != null;

        if (!hasA && !hasB) return;

        if (hasA && !hasB) { ExecuteEvolution(selectedCreature.evolvedFormPrefab); return; }
        if (!hasA && hasB) { ExecuteEvolution(selectedCreature.evolvedFormPrefabB); return; }

        if (evolutionChoiceUI == null)
        {
            Debug.LogWarning("GameManager: no EvolutionChoiceUI assigned — falling back to option A.");
            ExecuteEvolution(selectedCreature.evolvedFormPrefab);
            return;
        }

        evolutionChoiceUI.Show(
            selectedCreature.evolvedFormPrefab,
            selectedCreature.evolutionSpriteA,
            selectedCreature.evolutionLabelA,
            selectedCreature.evolvedFormPrefabB,
            selectedCreature.evolutionSpriteB,
            selectedCreature.evolutionLabelB
        );
    }

    public void ExecuteEvolution(GameObject chosenPrefab)
    {
        if (chosenPrefab == null || selectedCreature == null) return;
        StartCoroutine(EvolveRoutine(chosenPrefab));
    }

    private IEnumerator EvolveRoutine(GameObject chosenPrefab)
    {
        Creature evolving = selectedCreature;
        yield return StartCoroutine(evolving.PlayEvolveAnimation());

        Tile tile = evolving.currentTile;
        int player = evolving.assignedPlayer;
        GameObject oldObj = evolving.gameObject;

        Creature evolved = Instantiate(chosenPrefab).GetComponent<Creature>();
        evolved.assignedPlayer = player;
        evolved.isEvolved = true;

        tile.currentCreatureOnTile = null;
        Destroy(oldObj);

        evolved.Initialize(tile);
        selectedCreature = evolved;

        deathPoints--;
        SpendAction(1);
        UpdateDeathPointsUI();
        Debug.Log("Evolved into: " + evolved.name);

        FinishAction(); // deselects + hides ActionUI, same as every other action
    }

    public void ClearSelectionState()
    {
        selectedCreature = null;
        currentMode = ActionMode.None;
        pendingMove = null;
        BlackBoard.gridManager.ResetGridHighlights();
        if (Tile.selectedTile != null)
        {
            Tile.selectedTile.SetMaterial(Tile.selectedTile.originalMaterial);
            Tile.selectedTile = null;
        }
        if (ActionUI.Instance != null)
            ActionUI.Instance.Hide();
    }

    public void CheckVictory()
    {
        Creature[] allCreatures = FindObjectsByType<Creature>(FindObjectsSortMode.None);
        Debug.Log("CheckVictory called. Creatures found: " + allCreatures.Length);

        for (int p = 0; p < amountOfPlayers; p++)
        {
            bool hasAny = false;
            bool allDead = true;

            foreach (Creature c in allCreatures)
            {
                if (!c.enabled) continue;

                if (c.assignedPlayer == p)
                {
                    hasAny = true;
                    if (c.health > 0)
                    {
                        allDead = false;
                        break;
                    }
                }
            }

            if (hasAny && allDead)
            {
                int winner = (p == 0) ? 1 : 0;
                Debug.Log("Victory detected for player " + winner);

                if (CreaturePreviewManager.Instance != null)
                    CreaturePreviewManager.Instance.HidePreview();
                else
                    Debug.LogWarning("CheckVictory: CreaturePreviewManager.Instance is null");

                CreatureUI.HideCurrentStats();

                if (LevelManager.Instance != null)
                    LevelManager.Instance.OnBattleVictory(winner);
                else
                    Debug.LogWarning("CheckVictory: LevelManager.Instance is null — victory not applied!");

                return;
            }
        }
    }
}