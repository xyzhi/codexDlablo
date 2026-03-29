using System;
using System.Collections.Generic;
using System.Linq;

namespace Wuxing.Config
{
    [Serializable]
    public class CharacterDatabase
    {
        public List<CharacterConfig> characters = new List<CharacterConfig>();

        public IReadOnlyList<CharacterConfig> Characters => characters;

        public CharacterConfig GetById(string id)
        {
            return characters.FirstOrDefault(character => character.Id == id);
        }
    }
}

