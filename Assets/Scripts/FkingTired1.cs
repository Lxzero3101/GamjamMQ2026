using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
public class FkingTired1 : MonoBehaviour
{
    [SerializeField] private string _nextSceneName = "NextScene";
    // Assign your BattleSystem reference in the Inspector (or find it automatically)
    public BattleSystem battleSystem;

    // Tracks how many times THIS prefab type has been placed across all instances
    private static int placementCount = 0;

    // Call this to reset count at the start of a new battle
    public static void ResetCount() => placementCount = 0;

    private void Start()
    {
        if (battleSystem == null)
            battleSystem = FindFirstObjectByType<BattleSystem>();

        placementCount++;
        StartCoroutine(ApplyEffect());
    }

    private IEnumerator ApplyEffect()
    {
        yield return null; // wait one frame to ensure everything is initialized

        Unit enemy = battleSystem.enemyUnit;
        BattleHUD enemyHUD = battleSystem.enemyHUD;


        if (placementCount >= 3)
        {
            // 3rd placement: instant kill (100% HP removal)
            enemy.currentHP = 0;
            battleSystem.dialogueText.text = "OBLITERATED the enemy!";
        }
        else
        {
            // Remove 30% of max HP
            int damage = Mathf.RoundToInt(enemy.maxHP * 0.30f);
            enemy.currentHP = Mathf.Max(0, enemy.currentHP - damage);
            battleSystem.dialogueText.text = "Enemy lost 30% HP!";
        }

        // Update the HP bar
        enemyHUD.SetHP(enemy.currentHP);
        // Counter-attack: only if enemy is still alive
if (enemy.currentHP > 0)
{
    yield return new WaitForSeconds(3f); // wait 3 seconds before counter

    int counterDamage = 0;

    if (placementCount == 1)
        counterDamage = Mathf.RoundToInt(battleSystem.playerUnit.maxHP * 0.50f);
    else if (placementCount == 2)
        counterDamage = Mathf.RoundToInt(battleSystem.playerUnit.maxHP * 0.40f);

    if (counterDamage > 0)
    {
        battleSystem.playerUnit.currentHP = Mathf.Max(0, battleSystem.playerUnit.currentHP - counterDamage);
        battleSystem.playerHUD.SetHP(battleSystem.playerUnit.currentHP);

        int percent = placementCount == 1 ? 50 : 40;
        battleSystem.dialogueText.text = "Enemy counter-attacks! You lost " + percent + "% HP!";
    }
}

        // Check if enemy is dead
       yield return new WaitForSeconds(1.5f);
if (enemy.currentHP <= 0)
{
    battleSystem.state = BattleState.WON;
    battleSystem.SendMessage("EndBattle");
    yield return new WaitForSeconds(2f);
    SceneManager.LoadScene(_nextSceneName);
}

         // Auto destroy prefab after 2 seconds
    Destroy(gameObject, 1f);
    }
   
}