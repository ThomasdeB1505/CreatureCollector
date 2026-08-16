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
    private bool battleStarted = false;
    private bool isCapturing = false;
    public void SetCapturing(bool value) => isCapturing = value;
    public Material[] playerMaterials;
    private bool victoryCheckQueued = false;

    [Header("Evolution")]
    public int evolutionUnlockTurn = 5; // global turn count before anyone can evolve
    private int globalTurnCount = 0;

    public EvolutionPopupUI evolutionPopupUI; // new popup, see below

    public bool EvolutionUnlocked => globalTurnCount >= evolutionUnlockTurn;

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

        if (currentPlayer == 0)
            globalTurnCount++;

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
        if (currentPlayer != 0) return; // block all human input during AI's turn
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
                    {
                        List<Tile> validTiles = pendingMove.GetValidTargetTiles(selectedCreature);
                        foreach (Tile t in validTiles)
                            t.SetMaterial(BlackBoard.gridManager.moveRangeMaterial);

                        if (Tile.hoveredTile != null && validTiles.Contains(Tile.hoveredTile))
                        {
                            foreach (Tile t in pendingMove.GetAffectedTiles(Tile.hoveredTile))
                                t.SetMaterial(BlackBoard.gridManager.aoeRadiusMaterial);
                        }
                    }
                    break;
            }
        }
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
    }

    public void OnPlayerCreatureDied()
    {
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
    public void TryEvolveSelected()
    {
        Debug.Log($"TryEvolve called. selected={selectedCreature}, evolved={selectedCreature?.isEvolvedThisCombat}, unlocked={EvolutionUnlocked}, actionsLeft={HasActionsLeft()}");
        if (selectedCreature == null) return;
        if (selectedCreature.isEvolvedThisCombat) return;
        if (!EvolutionUnlocked) return;
        if (!HasActionsLeft()) return;

        bool hasForm = selectedCreature.formEvolutionPrefab != null;
        bool hasEssence = selectedCreature.essenceEvolutionPrefab != null;
        if (!hasForm && !hasEssence) return;

        evolutionPopupUI.Show(selectedCreature); // shows both options if present, image + description, no cost
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
        GameObject originalSource = evolving.sourcePrefab; // remember for revert
        GameObject oldObj = evolving.gameObject;

        tile.currentCreatureOnTile = null;
        Destroy(oldObj);

        Creature evolved = Instantiate(chosenPrefab).GetComponent<Creature>();
        evolved.assignedPlayer = player;
        evolved.isEvolvedThisCombat = true;
        evolved.sourcePrefab = originalSource; // carry forward so revert still works after evolving
        evolved.Initialize(tile);

        selectedCreature = evolved;
        SpendAction(1);
        FinishAction();
    }

    public void RequestVictoryCheck()
    {
        if (victoryCheckQueued) return;
        victoryCheckQueued = true;
        StartCoroutine(DeferredVictoryCheck());
    }

    private IEnumerator DeferredVictoryCheck()
    {
        yield return new WaitForEndOfFrame();
        victoryCheckQueued = false;
        CheckVictory();
    }

    public void CheckVictory()
    {
        Creature[] allCreatures = FindObjectsByType<Creature>(FindObjectsSortMode.None);
        Debug.Log("CheckVictory called. Creatures found: " + allCreatures.Length);

        bool[] hasAny = new bool[amountOfPlayers];
        bool[] hasLiving = new bool[amountOfPlayers];

        foreach (Creature c in allCreatures)
        {
            int p = c.assignedPlayer;
            if (p < 0 || p >= amountOfPlayers) continue;

            hasAny[p] = true;
            if (!c.dead && c.health > 0)
                hasLiving[p] = true;
        }

        // Check player 0 first: if the player's team is wiped, that's a loss for them
        // even if the enemy also got wiped in the same action (e.g. mutual-kill self-destruct).
        for (int p = 0; p < amountOfPlayers; p++)
        {
            bool wiped = hasAny[p] && !hasLiving[p];
            if (wiped)
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