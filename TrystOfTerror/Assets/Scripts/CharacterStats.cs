public class CharacterStats
{
    public string Name { get; set; }
    public int Level { get; set; }

    public int health;

    public int stamina;

    public int morale;
    public CharacterAttributes Attributes { get; set; }

    public CharacterStats() //Default/testing
    {
        Name = "Joan";
        Level = 1;
        Attributes = new CharacterAttributes(2,2,2,2,2,2,2,2,2,2,2);
        health = 1 + Attributes.vigor.GetValue(); //base health + vigor
        stamina = 10 + Attributes.endurance.GetValue(); //base stamina + endurance
        morale = 5 + Attributes.willpower.GetValue();
    }

    //TODO more control over stats with new constructor
}
