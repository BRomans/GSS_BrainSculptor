using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BarController : MonoBehaviour
{
    public float CurrentPower { get => currentPower/100; }
    [SerializeField] private UnityEngine.UI.Image powerBar;
    [SerializeField] private float currentPower, maxPower;
    [SerializeField] private float increaseModifier, decreaseModifier;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("t"))
        {
            IncreaseBar();
        }
        else
        {
            DecreaseBar();
        }

        powerBar.fillAmount = currentPower / maxPower;
    }

    public void DecreaseBar()
    {
        currentPower -= decreaseModifier * Time.deltaTime;

        if (currentPower < 0)
        {
            currentPower = 0;
        }
    }

    public void IncreaseBar()
    {
        currentPower += increaseModifier * Time.deltaTime;

        if (currentPower > maxPower)
        {
            currentPower = maxPower;
        }
    }
}
