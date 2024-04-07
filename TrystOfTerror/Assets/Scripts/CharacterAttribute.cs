using System;
using System.Collections.Generic;
public class CharacterAttribute : IComparable<CharacterAttribute>
{
    protected int attributeValue;

    public CharacterAttribute(int val) 
    {
        attributeValue = val;
    }

    public int GetValue() 
    {
        return attributeValue;
    }

    public int CompareTo(CharacterAttribute other)
    {
        //If other is not a valid object reference, this instance is greater.
        if (other == null) return 1;

        //-1 this.attributeValue is less than the other
        //0 - this.attributeValie is equal to the other
        //1 - this.attributeValue is greater than the other
        return attributeValue.CompareTo(other.attributeValue);
    }

    // Define the is greater than operator.
    public static bool operator >(CharacterAttribute operand1, CharacterAttribute operand2)
    {
        return operand1.CompareTo(operand2) > 0;
    }

    // Define the is less than operator.
    public static bool operator <(CharacterAttribute operand1, CharacterAttribute operand2)
    {
        return operand1.CompareTo(operand2) < 0;
    }

    // Define the is greater than or equal to operator.
    public static bool operator >=(CharacterAttribute operand1, CharacterAttribute operand2)
    {
        return operand1.CompareTo(operand2) >= 0;
    }

    // Define the is less than or equal to operator.
    public static bool operator <=(CharacterAttribute operand1, CharacterAttribute operand2)
    {
        return operand1.CompareTo(operand2) <= 0;
    }
}
