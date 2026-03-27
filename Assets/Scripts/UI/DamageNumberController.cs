using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class DamageNumberController : MonoBehaviour
{

    public static DamageNumberController numberControllerInstance;
    [SerializeField] private Transform numberCanvas;
    [SerializeField] private GameObject numberPrefab;
    private List<DamageNumber> damageNumbers = new List<DamageNumber>();
    private ConcurrentQueue<DamageNumber> availableDamageNumbers = new ConcurrentQueue<DamageNumber>();


    private void Awake()
    {
        if (numberControllerInstance != null && numberControllerInstance != this)
        {
            Destroy(gameObject);
            return;
        }
        numberControllerInstance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ShowDamageNumber(10, Vector3.one);
        }
    }

    public void ShowDamageNumber(float totalDamage, Vector3 location)
    {
        DamageNumber number = GetDamageNumber();
        number?.SetupNumber(totalDamage, location);
    }

    private DamageNumber GetDamageNumber()
    {
        if (availableDamageNumbers.Count == 0)
        {
            DamageNumber newNumber = InitialDamageNumber();
            if (!damageNumbers.Contains(newNumber) && !availableDamageNumbers.Contains(newNumber))
            {
                damageNumbers.Add(newNumber);
                availableDamageNumbers.Enqueue(newNumber);
            }
        }
        var isAvailable = availableDamageNumbers.TryDequeue(out var damageNumber);
        Debug.Log("GetDamageNumber isAvailable = " + isAvailable);
        return isAvailable ? damageNumber : null;
    }

    private DamageNumber InitialDamageNumber()
    {
        var number = Instantiate(numberPrefab, transform.position, Quaternion.identity, numberCanvas);
        number.SetActive(false);
        return number.GetComponent<DamageNumber>();
    }

}
