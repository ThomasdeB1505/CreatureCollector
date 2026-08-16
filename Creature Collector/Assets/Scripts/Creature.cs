using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Creature : Unit
{
    public int assignedPlayer;

    public int moveRange;
    public int moveMinRange = 1; // 1 matches the old behavior
    public int moveActionCost;

    public int attackRange;
    public int attackMinRange = 1; // 1 matches the old behavior
    public int attackDamage;
    public int attackActionCost;

    public int health;
    public int maxHealth = 100; // set to match starting health in the Inspector
    public bool dead;

    private CreatureHealthUI healthUI;
    public Material playerOneMaterial;
    public Material playerTwoMaterial;
    public GameObject visualAlive, visualDead;
    public GameObject sourcePrefab;
    public Sprite portrait;
    [TextArea]
    public string description;
    public List<CreatureMove> moves;
    [Header("Crafting Identity")]
    public FormType form;
    public EssenceType essence;

    [Header("Evolution (in-combat, temporary)")]
    public bool isEvolvedThisCombat = false;
    public GameObject formEvolutionPrefab;
    public Sprite formEvolutionSprite;
    public string formEvolutionDescription; // was "label" - now a fuller explanation of what it does

    public GameObject essenceEvolutionPrefab;
    public Sprite essenceEvolutionSprite;
    public string essenceEvolutionDescription;

    [Header("Enemy Essence Drop")]
    public EssenceType essenceDropType;

    [Header("Legacy Team-Color Shader")]
    [Tooltip("Off by default now that creatures use sprites. Turn back on to re-enable the red/blue material swap in Initialize().")]
    public bool useTeamColorShader = false;

    // ── Defensive Stance state ──────────────────────────────────────────
    public bool inDefensiveStance = false;
    [Tooltip("Damage multiplier while in stance - not specified in your spec, tune freely")]
    public float defensiveStanceDamageMultiplier = 0.5f;
    private Tile defensiveBlockedTile;

    public virtual void Initialize(Tile _startingTile)
    {
        currentTile = _startingTile;
        transform.position = currentTile.transform.position;
        gridPosition = _startingTile.gridPosition;
        _startingTile.currentCreatureOnTile = this;
        visualAlive.SetActive(true);
        visualDead.SetActive(false);
        healthUI = GetComponent<CreatureHealthUI>();
        if (healthUI != null)
            healthUI.Initialize(this, health);
        if (useTeamColorShader)
        {
            Renderer r = visualAlive.GetComponent<Renderer>()
                      ?? visualAlive.GetComponentInChildren<Renderer>();
            if (BlackBoard.gameManager.playerMaterials != null
                && BlackBoard.gameManager.playerMaterials.Length > assignedPlayer)
            {
                Material mat = BlackBoard.gameManager.playerMaterials[assignedPlayer];
                foreach (Renderer rend in visualAlive.GetComponentsInChildren<Renderer>())
                    rend.material = mat;
            }
        }
        Debug.Log("Spawned: " + gameObject.name + " at " + _startingTile.gridPosition);
        transform.rotation = Quaternion.Euler(0, assignedPlayer == 0 ? 90 : -90, 0);
    }

    public override void Moveto(Vector3 position, Tile _tile)
    {
        base.Moveto(position, _tile);
        _tile.currentCreatureOnTile = this;
        _tile.OnCreatureEntered(this);
    }

    public virtual void Attack(Creature _target)
    {
        if (_target.assignedPlayer == assignedPlayer)
            FriendlyAttack();
        else
            _target.TakeDamage(attackDamage);
    }

    public virtual void FriendlyAttack() { }

    public virtual void TakeDamage(int _damage)
    {
        if (inDefensiveStance)
            _damage = Mathf.RoundToInt(_damage * defensiveStanceDamageMultiplier);

        health -= _damage;
        if (healthUI != null)
            healthUI.UpdateHearts(health);
        if (health <= 0)
            Die();
    }

    public void RefreshHealthUI()
    {
        if (healthUI != null)
            healthUI.UpdateHearts(health);
    }

    // In Creature.Die()
    public virtual void Die()
    {
        dead = true;
        visualAlive.SetActive(false);
        visualDead.SetActive(true);
        if (assignedPlayer == 0)
            BlackBoard.gameManager.OnPlayerCreatureDied();

        BlackBoard.gameManager.RequestVictoryCheck();   // was: CheckVictory()

        if (assignedPlayer == 1)
            LevelManager.Instance.OnEnemyCreatureDied(this);
    }

    // ── Defensive Stance ─────────────────────────────────────────────────
    public void EnterDefensiveStance(Tile secondTile)
    {
        inDefensiveStance = true;
        defensiveBlockedTile = secondTile;
        if (secondTile != null)
            secondTile.blocked = true;
        StartCoroutine(PlayDefensiveStanceEnterAnimation());
    }

    public void ExitDefensiveStance()
    {
        if (!inDefensiveStance) return;
        inDefensiveStance = false;
        if (defensiveBlockedTile != null)
            defensiveBlockedTile.blocked = false;
        defensiveBlockedTile = null;
        StartCoroutine(PlayDefensiveStanceExitAnimation());
    }

    // ── Animation hooks - override per creature subclass, no-op by default ──
    public virtual IEnumerator PlayMoveAnimation() { yield break; }
    public virtual IEnumerator PlayAttackAnimation(Creature target) { yield break; }
    public virtual IEnumerator PlayEvolveAnimation() { yield break; }
    public virtual IEnumerator PlayDefensiveStanceEnterAnimation() { yield break; }
    public virtual IEnumerator PlayDefensiveStanceExitAnimation() { yield break; }
}