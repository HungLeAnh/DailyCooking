using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameDefine : MonoBehaviour
{
    #region Grid
    public const int GridSize = 5;
    public const string GridArrayDataInit = "[{\"Dir\":0,\"Origin\":{\"x\":0,\"y\":0},\"PlacedObjectTypeSOGuid\":\"127792b0de500fd4a972be0796d3f7d0\"},{\"Dir\":1,\"Origin\":{\"x\":0,\"y\":1},\"PlacedObjectTypeSOGuid\":\"667d87b3a7db4ed4ab2f0a0eb500f026\"},{\"Dir\":1,\"Origin\":{\"x\":0,\"y\":2},\"PlacedObjectTypeSOGuid\":\"bc571c2f81ae71245b19e8a3748b8b72\"},{\"Dir\":1,\"Origin\":{\"x\":0,\"y\":3},\"PlacedObjectTypeSOGuid\":\"74268350f66e75a42929093c253fe95f\"},{\"Dir\":0,\"Origin\":{\"x\":1,\"y\":4},\"PlacedObjectTypeSOGuid\":\"9389074fac07927409864b8777f88ba4\"},{\"Dir\":0,\"Origin\":{\"x\":2,\"y\":4},\"PlacedObjectTypeSOGuid\":\"dc63d3d705fb302488edb887a61af857\"},{\"Dir\":0,\"Origin\":{\"x\":3,\"y\":0},\"PlacedObjectTypeSOGuid\":\"8f3e91f1035d46b47b48904b37264d63\"},{\"Dir\":2,\"Origin\":{\"x\":3,\"y\":4},\"PlacedObjectTypeSOGuid\":\"08873bba0e59b0c40bbe5c4a20a9e947\"}]";
    #endregion

    #region Player State Machine
    public const float MIN_DISTANCE_TO_TARGET = 0.05f;
    public const float INTERACT_DISTANCE_MAX = 999f;
    #endregion
}
