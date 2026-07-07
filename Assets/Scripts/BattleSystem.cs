using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum BattleState { START, PLAYERTURN, ENEMYTURN, WON, LOST }

public class BattleSystem : MonoBehaviour
{
	[Header("VFX Prefabs")]
	[SerializeField] private GameObject _swordSlashPrefab;

	public GameObject playerPrefab;
	public GameObject enemyPrefab;

	public Transform playerBattleStation;
	public Transform enemyBattleStation;

	public Unit playerUnit;
	public Unit enemyUnit;

	public TMP_Text dialogueText;
	public BattleHUD playerHUD;
	public BattleHUD enemyHUD;

	public BattleState state;

    // Start is called before the first frame update
    void Start()
    {
		state = BattleState.START;
		StartCoroutine(SetupBattle());
    }

	IEnumerator SetupBattle()
	{
		GameObject playerGO = Instantiate(playerPrefab, playerBattleStation);
		playerUnit = playerGO.GetComponent<Unit>();

		GameObject enemyGO = Instantiate(enemyPrefab, enemyBattleStation);
		enemyUnit = enemyGO.GetComponent<Unit>();

		dialogueText.text = "The collided " + enemyUnit.unitName + " approaches...";

		playerHUD.SetHUD(playerUnit);
		enemyHUD.SetHUD(enemyUnit);

		yield return new WaitForSeconds(2f);

		state = BattleState.PLAYERTURN;
		PlayerTurn();
		FkingTired1.ResetCount();
		FkingTired2.ResetCount();
	}

	IEnumerator PlayerAttack()
{
    dialogueText.text = "You swing your sword!";

    if (_swordSlashPrefab != null)
    {
        GameObject slashGO = Instantiate(_swordSlashPrefab, enemyBattleStation.position, Quaternion.identity);
        SwordSlashEffect slashEffect = slashGO.GetComponent<SwordSlashEffect>();

        if (slashEffect != null)
        {
            slashEffect.Initialize(playerUnit.damage);
        }
    }

    yield break; // stop here, OnSwordHit() takes over from collision
}

	IEnumerator EnemyTurn()
	{
		dialogueText.text = enemyUnit.unitName + " attacks!";

		yield return new WaitForSeconds(1f);

		bool isDead = playerUnit.TakeDamage(enemyUnit.damage);

		playerHUD.SetHP(playerUnit.currentHP);

		yield return new WaitForSeconds(1f);

		if(isDead)
		{
			state = BattleState.LOST;
			EndBattle();
		} else
		{
			state = BattleState.PLAYERTURN;
			PlayerTurn();
		}

	}

	void EndBattle()
	{
		if(state == BattleState.WON)
		{
			dialogueText.text = "You won the battle!";
		} else if (state == BattleState.LOST)
		{
			dialogueText.text = "You were defeated.";
		}
	}

	void PlayerTurn()
	{
		dialogueText.text = "Choose an action: Press F to choose skill to attack";
	}

	IEnumerator PlayerHeal()
	{
		playerUnit.Heal(5);

		playerHUD.SetHP(playerUnit.currentHP);
		dialogueText.text = "You feel renewed strength!";

		yield return new WaitForSeconds(2f);

		state = BattleState.ENEMYTURN;
		StartCoroutine(EnemyTurn());
	}

	public void OnAttackButton()
	{
		if (state != BattleState.PLAYERTURN)
			return;

		StartCoroutine(PlayerAttack());
	}

	public void OnHealButton()
	{
		if (state != BattleState.PLAYERTURN)
			return;

		StartCoroutine(PlayerHeal());
	}
	public void OnSwordHit(int damage)
{
    enemyUnit.TakeDamage(damage);
    enemyHUD.SetHP(enemyUnit.currentHP);
    StartCoroutine(AfterSwordHit());
}

private IEnumerator AfterSwordHit()
{
    yield return new WaitForSeconds(1.5f);

    if (enemyUnit.currentHP <= 0)
    {
        state = BattleState.WON;
        EndBattle();
    }
    else
    {
        state = BattleState.ENEMYTURN;
        StartCoroutine(EnemyTurn());
    }
}

}
