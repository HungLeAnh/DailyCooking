using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameDefine : MonoBehaviour
{
    #region Grid
    public const int GridSize = 5;
    public const string GridArrayDataInit =
    "[[[{\"Dir\": 1,\"Origin\": {  \"x\": 0,  \"y\": 0},\"PlacedObjectTypeSOGuid\": \"e735350232f8f564a9bf514f8e3b0d55\",\"Type\": 2},{\"Dir\": 0,\"Origin\": {  \"x\": 0,  \"y\": 0},\"PlacedObjectTypeSOGuid\": \"e735350232f8f564a9bf514f8e3b0d55\",\"Type\": 2},{\"Dir\": 0,\"Origin\": {  \"x\": 0,  \"y\": 0},\"PlacedObjectTypeSOGuid\": \"127792b0de500fd4a972be0796d3f7d0\",\"Type\": 0}],[{\"Dir\": 1,\"Origin\": {  \"x\": 0,  \"y\": 1},\"PlacedObjectTypeSOGuid\": \"e735350232f8f564a9bf514f8e3b0d55\",\"Type\": 2},{\"Dir\": 1,\"Origin\": {  \"x\": 0,  \"y\": 1},\"PlacedObjectTypeSOGuid\": \"667d87b3a7db4ed4ab2f0a0eb500f026\",\"Type\": 0}],[{\"Dir\": 1,\"Origin\": {  \"x\": 0,  \"y\": 2},\"PlacedObjectTypeSOGuid\": \"e735350232f8f564a9bf514f8e3b0d55\",\"Type\": 2},{\"Dir\": 0,\"Origin\": {  \"x\": 0,  \"y\": 2},\"PlacedObjectTypeSOGuid\": \"bc571c2f81ae71245b19e8a3748b8b72\",\"Type\": 0}],[{\"Dir\": 1,\"Origin\": {  \"x\": 0,  \"y\": 3},\"PlacedObjectTypeSOGuid\": \"e735350232f8f564a9bf514f8e3b0d55\",\"Type\": 2},{\"Dir\": 1,\"Origin\": {  \"x\": 0,  \"y\": 3},\"PlacedObjectTypeSOGuid\": \"74268350f66e75a42929093c253fe95f\",\"Type\": 0}],[{\"Dir\": 1,\"Origin\": {  \"x\": 0,  \"y\": 4},\"PlacedObjectTypeSOGuid\": \"e735350232f8f564a9bf514f8e3b0d55\",\"Type\": 2},{\"Dir\": 2,\"Origin\": {  \"x\": 0,  \"y\": 4},\"PlacedObjectTypeSOGuid\": \"e735350232f8f564a9bf514f8e3b0d55\",\"Type\": 2},{\"Dir\": 0,\"Origin\": {  \"x\": 0,  \"y\": 4},\"PlacedObjectTypeSOGuid\": \"bc571c2f81ae71245b19e8a3748b8b72\",\"Type\": 0}]],[[{\"Dir\": 0,\"Origin\": {  \"x\": 1,  \"y\": 0},\"PlacedObjectTypeSOGuid\": \"e735350232f8f564a9bf514f8e3b0d55\",\"Type\": 2}],null,null,null,[{\"Dir\": 2,\"Origin\": {  \"x\": 1,  \"y\": 4},\"PlacedObjectTypeSOGuid\": \"e735350232f8f564a9bf514f8e3b0d55\",\"Type\": 2},{\"Dir\": 0,\"Origin\": {  \"x\": 1,  \"y\": 4},\"PlacedObjectTypeSOGuid\": \"b28a5fed1f54f2948aa1cc6aa79f06d6\",\"Type\": 0}]],[[{\"Dir\": 0,\"Origin\": {  \"x\": 2,  \"y\": 0},\"PlacedObjectTypeSOGuid\": \"6acc2969bc480cd4386b40bd75c9a691\",\"Type\": 2}],null,null,null,[{\"Dir\": 2,\"Origin\": {  \"x\": 2,  \"y\": 4},\"PlacedObjectTypeSOGuid\": \"e735350232f8f564a9bf514f8e3b0d55\",\"Type\": 2},{\"Dir\": 0,\"Origin\": {  \"x\": 2,  \"y\": 4},\"PlacedObjectTypeSOGuid\": \"b28a5fed1f54f2948aa1cc6aa79f06d6\",\"Type\": 0}]],[[{\"Dir\": 0,\"Origin\": {  \"x\": 2,  \"y\": 0},\"PlacedObjectTypeSOGuid\": \"6acc2969bc480cd4386b40bd75c9a691\",\"Type\": 2}],[{\"Dir\": 0,\"Origin\": {  \"x\": 3,  \"y\": 1},\"PlacedObjectTypeSOGuid\": \"384226191b7471b4b82fd751d66b5757\",\"Type\": 1}],null,null,[{\"Dir\": 2,\"Origin\": {  \"x\": 3,  \"y\": 4},\"PlacedObjectTypeSOGuid\": \"e735350232f8f564a9bf514f8e3b0d55\",\"Type\": 2}]],[[{\"Dir\": 0,\"Origin\": {  \"x\": 4,  \"y\": 0},\"PlacedObjectTypeSOGuid\": \"e735350232f8f564a9bf514f8e3b0d55\",\"Type\": 2},{\"Dir\": 3,\"Origin\": {  \"x\": 4,  \"y\": 0},\"PlacedObjectTypeSOGuid\": \"e735350232f8f564a9bf514f8e3b0d55\",\"Type\": 2}],[{\"Dir\": 0,\"Origin\": {  \"x\": 3,  \"y\": 1},\"PlacedObjectTypeSOGuid\": \"384226191b7471b4b82fd751d66b5757\",\"Type\": 1},{\"Dir\": 3,\"Origin\": {  \"x\": 4,  \"y\": 1},\"PlacedObjectTypeSOGuid\": \"e735350232f8f564a9bf514f8e3b0d55\",\"Type\": 2}],[{\"Dir\": 3,\"Origin\": {  \"x\": 4,  \"y\": 2},\"PlacedObjectTypeSOGuid\": \"e735350232f8f564a9bf514f8e3b0d55\",\"Type\": 2}],[{\"Dir\": 3,\"Origin\": {  \"x\": 4,  \"y\": 3},\"PlacedObjectTypeSOGuid\": \"e735350232f8f564a9bf514f8e3b0d55\",\"Type\": 2}],[{\"Dir\": 2,\"Origin\": {  \"x\": 4,  \"y\": 4},\"PlacedObjectTypeSOGuid\": \"e735350232f8f564a9bf514f8e3b0d55\",\"Type\": 2},{\"Dir\": 3,\"Origin\": {  \"x\": 4,  \"y\": 4},\"PlacedObjectTypeSOGuid\": \"e735350232f8f564a9bf514f8e3b0d55\",\"Type\": 2}]]    ]";

    #endregion

    #region Player
    public const string PlayerCharacter = "{\"Back\": -1,\"Base body\": 0,\"Eye\": 13,\"Face\": -1,\"Gloves\": -1,\"Hair\": -1,\"Hat\": -1,\"Mustache\": -1,\"Outfit\": -1,\"Pants\": -1,\"Shirts\": -1,\"Shoe\": -1,\"Horn\": -1}";
    #endregion

    #region Emotion
    public const float EMOTION_DURATION = 15f;
    #endregion
    #region Bot
    public const float TIP_PERCENTAGE = 0.7f;
    #endregion
}
