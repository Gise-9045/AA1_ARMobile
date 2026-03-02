using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[Serializable]
public class Recipes
{
    public List<Ingredients> ingredients;
}
[Serializable]
public class Ingredients
{
    public string name;
    public float quantity;

}