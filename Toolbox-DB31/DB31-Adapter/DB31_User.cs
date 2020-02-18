using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolbox_DB31.DB31_Adapter
{
    public class DB31_User
    {
        public enum Enum_Department { Inspector, Operator, Maintainer };


        public string UserName;
        public Enum_Department Department; 

        public string Input_UserName;
        public string Input_Password;
        public Enum_Department Input_Department;

        public DB31_User()
        {
            //Input_UserName = "test";
            //Input_Password = "OK";
        }
        public bool Verify()
        {
            switch( Input_Department)
            {
                case Enum_Department.Inspector:
                    if(Input_UserName == "admin" && Input_Password =="admin")
                    {
                        UserName = Input_UserName;
                        Department = Input_Department;

                        Reset_Input();

                        return true;
                    }
                    break;
                case Enum_Department.Operator:
                    break;
                case Enum_Department.Maintainer:
                    break;
            }

           
            return false;
        }

        void Reset_Input()
        {
            Input_UserName = Input_Password = "";
        }
    }
}
