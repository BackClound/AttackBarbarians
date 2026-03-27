using System;
using TMPro;
using UnityEngine;

public class DamageNumber : MonoBehaviour
{
    public static Action<GameObject> resetNumberText;
    [SerializeField] private TextMeshProUGUI numberText;
    private int damageValue;
    [SerializeField] private float intervalThreshold;
    [SerializeField] private float intervalTime;
    [SerializeField] private float floatSpeed;
    [SerializeField] private float scaleSpeed;
    private Vector3 originalScale;

    private void Awake()
    {
        if (numberText == null)
        {
            numberText = GetComponent<TextMeshProUGUI>();
        }

        intervalTime = intervalThreshold;
    }

    private void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        intervalTime -= Time.deltaTime;
        if (intervalTime < 0)
        {
            DisableNumberComponent();
            return;
        }
        transform.position = transform.position + Vector3.up * floatSpeed * Time.deltaTime;
        transform.localScale += Vector3.one * scaleSpeed * Time.deltaTime;
    }

    public void SetupNumber(float totalDamage, Vector2 location)
    {
        if (numberText != null)
        {
            var damage = Mathf.RoundToInt(totalDamage);
            numberText.text = damage.ToString();
            transform.position = location;
            intervalTime = intervalThreshold;
            gameObject.SetActive(true);
        }
    }

    private void DisableNumberComponent()
    {
        gameObject.SetActive(false);
        numberText.text = "";
        damageValue = 0;
        intervalTime = intervalThreshold;
        transform.localScale = originalScale;
        resetNumberText?.Invoke(gameObject);
    }
}
