using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Equipped
{
    Axe,
    Machete,
    Baton,
    Katana,
    m92,
    ak47
};

public enum Equipped_Type
{
    Melee,
    Gun
}

public class PlayerAction : MonoBehaviour
{
    [Header("Player Settings")]
    public static PlayerAction instance;
    public int player_hp = 100;
    public Animator animator;
    public GameObject WeaponSlot; // Slot where the weapon will be equipped
    public List<GameObject> Weapons; // List of all weapon prefabs
    public GameObject selectedWeapon;
    public Equipped PlayerInventory = Equipped.Axe; // Currently equipped weapon
    public Equipped_Type PlayerInventoryType = Equipped_Type.Melee;
    public bool inAttack = false;
    public bool inSwitching = false;
    public bool inConstrain = false;
    public bool inAim = false;

    [Header("Mouse Settings")]
    public float maxCPS = 5f; // Maximum CPS (Clicks per second)
    private float timeBetweenClicks; // Time in seconds between clicks
    private float lastClickTime = 0f;

    void Start()
    {
        PlayerInventory = Equipped.Axe; // Set default weapon to Machete
        PlayerInventoryType = Equipped_Type.Melee;
        EquipWeapon(PlayerInventory); // Equip initial weapon at start
        timeBetweenClicks = 1f / maxCPS;
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    public void ReceiveHit(int damage)
    {
        if (inConstrain)
        {
            // to add
        } else
        {
            player_hp = Mathf.Max(player_hp - damage, 0);
            animator.Play("Hit", 2);
        }
    }


    void Update()
    {
        //Debug.Log($"player inv type: {PlayerInventoryType}");
        // OnTriggerEnter(gameObject.GetComponent<CharacterController>());

        AnimatorStateInfo Weapons_layer = animator.GetCurrentAnimatorStateInfo(1); // Layer 1

        if (Weapons_layer.IsName("Attack"))
        {
            float progress = Weapons_layer.normalizedTime % 1f; // Make sure it's between 0 and 1

            if (progress > 0f && progress < 0.5f)
            {
                inAttack = true;
            } else
            {
                inAttack = false; 
            }
        }

        #if UNITY_STANDALONE || UNITY_EDITOR
            if (!TouchScreenCameraMovement.inAndroid)
            {
                if (Input.GetMouseButtonDown(0)) 
                {
                    Attack();
                }

                if (Input.GetMouseButtonDown(1) && !inConstrain) 
                {
                    Debug.Log(PlayerInventory);
                    if (PlayerInventoryType == Equipped_Type.Melee)
                    {
                        Block();
                    } 
                } 
                else if (Input.GetMouseButtonUp(1))
                {
                    CancelConstrain();
                }
            }
        #endif

        if (!inConstrain)
        {
            // Melee
            ApplyWeapon(KeyCode.Alpha1, Equipped.Axe);
            ApplyWeapon(KeyCode.Alpha2, Equipped.Machete);
            ApplyWeapon(KeyCode.Alpha3, Equipped.Baton);
            ApplyWeapon(KeyCode.Alpha4, Equipped.Katana);
            // Guns
            ApplyWeapon(KeyCode.Alpha5, Equipped.m92);
            ApplyWeapon(KeyCode.Alpha6, Equipped.ak47);

            
            
        }
        
        if (PlayerInventoryType == Equipped_Type.Gun)
        {
            inAim = true;
            animator.SetBool("InGunAim", inAim);
        } else
        {
            inAim = false;
            animator.SetBool("InGunAim", inAim);
        }

        
        
    }

    void ApplyWeapon(KeyCode key, Equipped weapon)
    {
        if (Input.GetKeyDown(key) && PlayerInventory != weapon)
        {
            inSwitching = true;
            animator.CrossFade("Draw", 0.2f, 4); // Draw is the state name, it has to match
            StartCoroutine(WaitAndEquip("Draw", weapon, 4)); // its "4" for being inclusive, Layer 5 "Arms" has draw animations. 
        }
    }


    public void EquipWeapon(Equipped equip)
    {
        //Debug.Log("Equipping wep...");
        foreach (Transform child in WeaponSlot.transform)
        {
            child.gameObject.SetActive(false); // Disable all weapons
        }

        PlayerInventory = equip;
        
        selectedWeapon = null;
        switch (equip)
        {
            case Equipped.Axe:
                selectedWeapon = Weapons[0].gameObject;
                PlayerInventoryType = Equipped_Type.Melee;
                break;
            case Equipped.Machete:
                selectedWeapon = Weapons[1].gameObject;
                PlayerInventoryType = Equipped_Type.Melee;
                break;
            case Equipped.Baton:
                selectedWeapon = Weapons[2].gameObject;
                PlayerInventoryType = Equipped_Type.Melee;
                break; 
            case Equipped.Katana:
                selectedWeapon = Weapons[3].gameObject;
                PlayerInventoryType = Equipped_Type.Melee;
                break;
            case Equipped.m92:
                selectedWeapon = Weapons[4].gameObject;
                PlayerInventoryType = Equipped_Type.Gun;
                AimPistol();
                break;
            case Equipped.ak47:
                selectedWeapon = Weapons[5].gameObject;
                PlayerInventoryType = Equipped_Type.Gun;
                AimRifle();
                break;
        }

        // Enable the selected weapon
        if (selectedWeapon != null)
        {
            selectedWeapon.SetActive(true);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Portal"))
        {
            Debug.Log("Player entered the portal!");

            if (SceneManager.GetActiveScene().name == "hg_field")
            {
                SceneManager.LoadScene("hg_training");
            }
            else
            {
                SceneManager.LoadScene("hg_field");
            }
            
        }
    }

    IEnumerator WaitAndEquip(string animName, Equipped weapon, int layer)
    {
        // Wait until the requested animation starts playing on the given layer
        while (!animator.GetCurrentAnimatorStateInfo(layer).IsName(animName))
            yield return null;

        // Now wait until the animation has completed (normalizedTime >= 1)
        while (animator.GetCurrentAnimatorStateInfo(layer).IsName(animName) &&
            animator.GetCurrentAnimatorStateInfo(layer).normalizedTime < 0.5f)
        {
            yield return null;
        }

        inSwitching = false;
        EquipWeapon(weapon);
    }

    public void Attack()
    {
        if (Time.time - lastClickTime >= timeBetweenClicks)
        {
            animator.CrossFade("Attack", 0.2f, 1);
            lastClickTime = Time.time;
        }
    }

    public void Block()
    {
        inConstrain = true;
        animator.CrossFade("Block", 0.25f, 4);
        animator.SetBool("InConstrain", inConstrain);
    }

    public void AimPistol()
    {
        animator.CrossFade("AimPistol", 0.25f, 3);
        //inConstrain = true;
    }

    public void AimRifle()
    {
        animator.CrossFade("AimRifle", 0.25f, 3);
        //inConstrain = true;
    }

    public void CancelConstrain()
    {
        inConstrain = false;
        animator.SetBool("InConstrain", inConstrain);
    }



    // Android settings
    public void CycleWeaponAndroid()
    {
        // Get all enum values
        Equipped[] weapons = (Equipped[])System.Enum.GetValues(typeof(Equipped));

        // Increment current index
        int nextIndex = ((int)PlayerInventory + 1) % weapons.Length;

        // Update current weapon
        PlayerInventory = weapons[nextIndex];

        // Call the existing equip logic
        ApplyWeaponAndroid(PlayerInventory);
    }

    public void ApplyWeaponAndroid(Equipped weapon)
    {
        inSwitching = true;
        animator.CrossFade("Draw", 0.2f, 4);
        StartCoroutine(WaitAndEquip("Draw", weapon, 4));
    }



}
