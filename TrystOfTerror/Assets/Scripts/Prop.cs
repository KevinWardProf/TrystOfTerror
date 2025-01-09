using UnityEngine;

namespace Assets.Scripts
{
    public class Prop : MonoBehaviour
    {
        public bool needsTwoHandsToPickUp;
        private void Start()
        {
            if (gameObject.tag != "Prop")
            {
                gameObject.tag = "Prop";
            }
        }
    }
}
