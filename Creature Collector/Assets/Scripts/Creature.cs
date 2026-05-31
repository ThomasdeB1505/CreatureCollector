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

    public GameObject visualAlive, visualDead;


    public virtual void Initialize(Tile _startingTile)
    {
        currentTile = _startingTile;
        transform.position = currentTile.transform.position;
        gridPosition = _startingTile.gridPosition;
        _startingTile.currentCreatureOnTile = this;
        visualAlive.SetActive(true);
        visualDead.SetActive(false);
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
        if (health <= 0)
            Die();
    }

    public virtual void Die()
    {
        //swap to a death animation or visual or whatever
        dead = true;
        visualAlive.SetActive(false);
        visualDead.SetActive(true);
    }
}
