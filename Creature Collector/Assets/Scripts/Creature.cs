using Unity.VisualScripting;
using UnityEngine;

public class Creature : Unit
{
    public int assignedPlayer;

    public int moveRange;
    public int moveActionCost;
    public int attackRange;
    public int attackDamage;
    public int attackActionCost;
    public int health;
    public bool dead;
    private CreatureHealthUI healthUI;
    public GameObject evolvedFormPrefab; // assign in Inspector if this creature can evolve
    public bool isEvolved = false;
    public Material playerOneMaterial; // assign blue in inspector
    public Material playerTwoMaterial; // assign red in inspector

    public GameObject visualAlive, visualDead;
    public GameObject sourcePrefab;
    public Sprite portrait;


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
        // Apply correct material
        Renderer r = visualAlive.GetComponent<Renderer>()
                  ?? visualAlive.GetComponentInChildren<Renderer>();
        if (BlackBoard.gameManager.playerMaterials != null
            && BlackBoard.gameManager.playerMaterials.Length > assignedPlayer)
        {
            Material mat = BlackBoard.gameManager.playerMaterials[assignedPlayer];
            foreach (Renderer rend in visualAlive.GetComponentsInChildren<Renderer>())
                rend.material = mat;
        }
        Debug.Log("Spawned: " + gameObject.name + " at " + _startingTile.gridPosition);
        // Face correct direction
        transform.rotation = Quaternion.Euler(0, assignedPlayer == 0 ? 90 : -90, 0);
    }

    public override void Moveto(Vector3 position, Tile _tile)
    {
        base.Moveto(position, _tile);
        _tile.currentCreatureOnTile = this;
    }

    public virtual void Attack(Creature _target)
    {
        if (_target.assignedPlayer == assignedPlayer)
            FriendlyAttack();
        else
            _target.TakeDamage(attackDamage);
    }

    public virtual void FriendlyAttack()
    {
        
    }
    public virtual void TakeDamage(int _damage)
    {
        health -= _damage;
        if (healthUI != null)
            healthUI.UpdateHearts(health); // ADD
        if (health <= 0)
            Die();
    }

    public virtual void Die()
    {
        dead = true;
        visualAlive.SetActive(false);
        visualDead.SetActive(true);

        // Notify manager if this was a player 1 creature
        if (assignedPlayer == 0)
            BlackBoard.gameManager.OnPlayerCreatureDied();

        BlackBoard.gameManager.CheckVictory();
    }

}
