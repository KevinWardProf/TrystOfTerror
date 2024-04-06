public class CharacterAttributes /*: IComparable<CharacterStatistics>*/
{
    //TODO remove   
    //public int Vitality { get; set; }
    //public int Strength { get; set; }
    //public int Dexterity { get; set; }
    //public int Endurance { get; set; }
    //public int Agility { get; set; }
    //public int Perception { get; set; }
    //public int Intelligence { get; set; }
    //public int Charisma { get; set; }
    //public int Faith { get; set; }
    //public int Luck { get; set; }
    //public int Willpower { get; set; }
    //public int CompareTo(CharacterStatistics other) 
    //{
    //    return (other != null) ? 1 : -1;
    //}

    public CharacterAttribute vigor;
    public CharacterAttribute strength;
    public CharacterAttribute dexterity;
    public CharacterAttribute endurance;
    public CharacterAttribute agility;
    public CharacterAttribute perception;
    public CharacterAttribute intelligence;
    public CharacterAttribute charisma;
    public CharacterAttribute faith;
    public CharacterAttribute luck;
    public CharacterAttribute willpower;

    public CharacterAttributes() { }
    public CharacterAttributes(int vig, int str, int dex, int end, int agl, int per, int intl, int chr, int fth, int lck, int wil) 
    {
        vigor = new CharacterAttribute(vig);
        strength = new CharacterAttribute(str);
        dexterity = new CharacterAttribute(dex);
        endurance = new CharacterAttribute(end);
        agility = new CharacterAttribute(agl);
        perception = new CharacterAttribute(per);
        intelligence = new CharacterAttribute(intl);
        charisma = new CharacterAttribute(chr);
        faith = new CharacterAttribute(fth);
        luck = new CharacterAttribute(lck);
        willpower = new CharacterAttribute(wil);

    }
}