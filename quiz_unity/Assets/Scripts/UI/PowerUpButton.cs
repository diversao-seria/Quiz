using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpButton : MonoBehaviour
{
    private GameObject gameController;
    private PowerUpController powerUpController;

    public string powerUpName;

    // Start is called before the first frame update
    void Start()
    {
        gameController = GameObject.Find("GameController");
        powerUpController = gameController.GetComponent<PowerUpController>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void HandleClick()
    {
        /*if (name == "Water")
        {
            powerUpController.WaterPowerUp();
        }
        else if (name == "Air")
        {
            powerUpController.AirPowerUp();
        }
        else if (name == "Earth")
        {
            powerUpController.EarthPowerUp();
        }
        powerUpController.RemovePowerUp(this.gameObject);*/
        if (!powerUpController.powerActive)
        {
            powerUpController.UsePowerUp(powerUpName);
            powerUpController.RemovePowerUp(this.gameObject);
        }
    }
}
