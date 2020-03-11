using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolbox_DB31.DB31_Adapter
{
    public class DB31_User
    {
        public enum Enum_Department { Nobody,Inspector, Operator, Maintainer,Max_Num };
        public enum Enum_Action { Inspect_Image_Upload, Test_Image_Upload,Max_Num };

        bool[,] PrivilegeMatrix = new bool[(int)Enum_Department.Max_Num,(int)Enum_Action.Max_Num];

        public string UserName;
        public Enum_Department Department; 

        public string Input_UserName;
        public string Input_Password;
        public Enum_Department Input_Department;

        public DB31_User()
        {
            Department = Enum_Department.Nobody;
            //Input_UserName = "test";
            //Input_Password = "OK";

            for(int i=0;i<(int)Enum_Department.Max_Num;i++)
            {
                for(int j=0;j<(int)Enum_Action.Max_Num;j++)
                {
                    PrivilegeMatrix[i, j] = false;
                }
            }

            PrivilegeMatrix[(int)Enum_Department.Inspector, (int)Enum_Action.Inspect_Image_Upload] = true;
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
                    if (Input_UserName == "operator" && Input_Password == "operator")
                    {
                        UserName = Input_UserName;
                        Department = Input_Department;

                        Reset_Input();

                        return true;
                    }
                    break;
                case Enum_Department.Maintainer:
                    if (Input_UserName == "maintainer" && Input_Password == "maintainer")
                    {
                        UserName = Input_UserName;
                        Department = Input_Department;

                        Reset_Input();

                        return true;
                    }
                    break;
            }
            return false;
        }

        void Reset_Input()
        {
            Input_UserName = Input_Password = "";
        }

        public bool Privilege_Check(Enum_Action action)
        {
            //Test code
            return true;

            return PrivilegeMatrix[(int)Department, (int)action];
        }
    }
}
