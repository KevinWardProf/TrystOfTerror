using System;
using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    public string Name { get; set; }
    public int Level { get; set; }
    public int health;
    public int stamina;
    public int morale;
    public float throwForce;
    public float grabRange;
    public CharacterAttributes Attributes { get; set; }

    public void InitializeCharacterStats() //Default/testing
    {
        Name = "Joan";
        Level = 1;
        Attributes = new CharacterAttributes(2,2,2,2,2,2,2,2,2,2,2);
        health = 1 + Attributes.vigor.GetValue(); //base health + vigor
        stamina = 10 + Attributes.endurance.GetValue(); //base stamina + endurance
        morale = 5 + Attributes.willpower.GetValue();
        throwForce = 200 * Attributes.strength.GetValue(); //todo some better calculation
        grabRange = 1 + Attributes.dexterity.GetValue(); //todo some better calculation
    }

    //TODO more control over stats with new constructor
}
