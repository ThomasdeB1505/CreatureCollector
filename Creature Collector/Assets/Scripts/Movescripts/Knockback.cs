using UnityEngine;

[CreateAssetMenu(menuName = "Creature/Moves/Knockback")]
public class Knockback : CreatureMove
{
    public int damage = 10;

    public override void Execute(Creature user, Creature target)
    {
        target.TakeDamage(damage);

        Vector2Int dir = target.gridPosition - user.gridPosition;
        dir = new Vector2Int((int)Mathf.Sign(dir.x), (int)Mathf.Sign(dir.y));
        Vector2Int pushPos = target.gridPosition + dir;

        Tile pushTile = BlackBoard.gridManager.GetTileAt(pushPos);
        if (pushTile != null && pushTile.currentCreatureOnTile == null)
            target.Moveto(pushTile.transform.position, pushTile);
    }
}
