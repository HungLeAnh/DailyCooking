using Observer;
using UnityEngine;

namespace MVC
{
    [System.Serializable]
    public class BaseModel : Observable
    {
    }

    public class BaseController<M> where M : BaseModel
    {
        protected M Model;

        public virtual void Setup(M model)
        {
            Model = model;
        }
    }

    public class BaseView<M, C> : MonoBehaviour
        where M : BaseModel
        where C : BaseController<M>, new()
    {
        public M Model;
        protected C Controller;

        public virtual void Awake()
        {
            Controller = new C();
            Controller.Setup(Model);
        }
    }
}
