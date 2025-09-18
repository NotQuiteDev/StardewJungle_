using UnityEngine;

public class AttackTrigger : MonoBehaviour
{
    private PlayerAttack playerAttack;

    void Start()
    {
        playerAttack = GetComponentInParent<PlayerAttack>();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if(playerAttack != null)
        {
            playerAttack.OnSwordHit(other);
        }
    }
}