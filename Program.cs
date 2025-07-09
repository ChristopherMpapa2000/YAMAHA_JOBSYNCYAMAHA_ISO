using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace JobSyncYAMAHA_EA
{
    internal class Program
    {
        public static string _Connection = ConfigurationSettings.AppSettings["ConnectionString"];
        public static string LogFile = ConfigurationSettings.AppSettings["LogFile"];
        public static void Log(String iText)
        {
            string pathlog = LogFile;
            String logFolderPath = System.IO.Path.Combine(pathlog, DateTime.Now.ToString("yyyyMMdd"));

            if (!System.IO.Directory.Exists(logFolderPath))
            {
                System.IO.Directory.CreateDirectory(logFolderPath);
            }
            String logFilePath = System.IO.Path.Combine(logFolderPath, DateTime.Now.ToString("yyyyMMdd") + ".txt");

            try
            {
                using (System.IO.StreamWriter outfile = new System.IO.StreamWriter(logFilePath, true))
                {
                    System.Text.StringBuilder sbLog = new System.Text.StringBuilder();

                    String[] listText = iText.Split('|').ToArray();

                    foreach (String s in listText)
                    {
                        sbLog.AppendLine($"[{DateTime.Now:HH:mm:ss}] {s}");
                    }

                    outfile.WriteLine(sbLog.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing log file: {ex.Message}");
            }
        }
        public class SP_GetEmployEa
        {
            public string CODEMPID { get; set; }
            public string NAMEMPT { get; set; }
            public string NAMEMPE { get; set; }
            public string CODPOS { get; set; }
            public string NAMPOS { get; set; }
            public string NAMCENTE { get; set; }
            public string NAMCENTHA { get; set; }
            public string NAMCENTENG { get; set; }
            public string CODCOMP { get; set; }
            public string department_t { get; set; }
            public string department_e { get; set; }
            public string division_t { get; set; }
            public string division_e { get; set; }
            public string TYPEMP { get; set; }
            public int STAEMP { get; set; }
            public string EMAIL { get; set; }
            public string codeHead { get; set; }
            public string NameHeadE { get; set; }
            public string NameHeadT { get; set; }
            public string CODCOMPR { get; set; }
            public string CODPOSPRE { get; set; }
            public string CODNATNL { get; set; }
        }
        static void Main(string[] args)
        {
            try
            {
                Log("================= Start =================");
                var _context = new YAMAHADataContext(_Connection);
                var store = "SP_GetEmployEa";
                var viewAllEA = ExcuteStoreQueryListAsync<SP_GetEmployEa>(store);
                if (viewAllEA.Count() > 0)
                {
                    Log("ConnectStore Success: " + viewAllEA.Count());
                    string UpEmployee = "UPDATE [dbo].[MSTEmployee] SET IsActive = 0 where EmployeeId <> 1";
                    using (SqlConnection connection = new SqlConnection(_Connection))
                    {
                        connection.Open();
                        using (SqlCommand command = new SqlCommand(UpEmployee, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                    foreach (var viewEmp in viewAllEA)
                    {
                        if (!string.IsNullOrEmpty(viewEmp.NAMPOS))
                        {
                            var positionQuery = _context.MSTPositions.Where(x => x.NameTh == viewEmp.NAMPOS).ToList();
                            if (positionQuery.Count() == 0)
                            {
                                var position = new MSTPosition();
                                position.CreatedDate = DateTime.Now;
                                position.ModifiedDate = DateTime.Now;
                                position.IsActive = true;
                                position.NameEn = viewEmp.NAMPOS.Replace(Environment.NewLine, "").Trim();
                                position.NameTh = viewEmp.NAMPOS.Replace(Environment.NewLine, "").Trim();
                                position.CreatedBy = "SYSTEM";
                                position.ModifiedBy = "SYSTEM";
                                position.CompanyCode = "TYM";
                                position.AccountId = 1;
                                _context.MSTPositions.InsertOnSubmit(position);
                            }
                        }

                        if (!string.IsNullOrEmpty(viewEmp.NAMCENTENG))
                        {
                            var deptQuery = _context.MSTDepartments.Where(x => x.NameEn == viewEmp.NAMCENTENG).ToList();
                            if (deptQuery.Count() == 0)
                            {
                                var dept = new MSTDepartment();
                                dept.CreatedDate = DateTime.Now;
                                dept.ModifiedDate = DateTime.Now;
                                dept.IsActive = true;
                                dept.NameEn = viewEmp.NAMCENTENG;
                                dept.NameTh = viewEmp.NAMCENTHA;
                                dept.CreatedBy = "SYSTEM";
                                dept.ModifiedBy = "SYSTEM";
                                dept.CompanyCode = "TYM";
                                dept.AccountId = 1;
                                dept.DepartmentCode = !string.IsNullOrEmpty(viewEmp.CODCOMP) ? viewEmp.CODCOMP : null;
                                _context.MSTDepartments.InsertOnSubmit(dept);
                            }
                        }
                        if (!string.IsNullOrEmpty(viewEmp.department_e))
                        {
                            var divQuery = _context.MSTDivisions.Where(x => x.NameEn == viewEmp.department_e).ToList();
                            if (divQuery.Count() == 0)
                            {
                                var div = new MSTDivision();
                                div.CreatedDate = DateTime.Now;
                                div.ModifiedDate = DateTime.Now;
                                div.IsActive = true;
                                div.NameEn = viewEmp.department_e;
                                div.NameTh = viewEmp.department_t;
                                div.CreatedBy = "SYSTEM";
                                div.ModifiedBy = "SYSTEM";
                                _context.MSTDivisions.InsertOnSubmit(div);
                            }
                            _context.SubmitChanges();
                        }
                    }

                    var updates = viewAllEA.Where(x => _context.MSTEmployees.Select(s => s.EmployeeCode).Contains(x.CODEMPID)).ToList();

                    var inserts = viewAllEA.Where(x => !_context.MSTEmployees.Select(s => s.EmployeeCode).Contains(x.CODEMPID)).ToList();

                    Console.WriteLine($"EMP UPDATE COUNT : {updates.Count()}");
                    Log($"EMP UPDATE COUNT : {updates.Count()}");
                    Console.WriteLine($"EMP INSERT COUNT : {inserts.Count()}");
                    Log($"EMP INSERT COUNT : {inserts.Count()}");

                    var empUpdates = _context.MSTEmployees.Where(x => updates.Select(s => s.CODEMPID).Contains(x.EmployeeCode)).ToList();

                    foreach (var update in empUpdates)
                    {
                        var mapper = updates.FirstOrDefault(x => update.EmployeeCode == x.CODEMPID);
                        update.Username = !string.IsNullOrEmpty(mapper.CODNATNL) && mapper.CODNATNL == "01" ? "CN" + mapper.CODEMPID : mapper.CODEMPID;
                        update.NameTh = mapper.NAMEMPT;
                        update.NameEn = mapper.NAMEMPE;
                        update.Email = mapper.EMAIL;
                        update.PositionId = _context.MSTPositions.FirstOrDefault(x => x.NameEn == mapper.NAMPOS || x.NameTh == mapper.NAMPOS)?.PositionId;
                        update.DepartmentId = _context.MSTDepartments.FirstOrDefault(x => x.NameEn == mapper.NAMCENTENG || x.NameTh == mapper.NAMCENTHA)?.DepartmentId;
                        update.DivisionId = _context.MSTDivisions.FirstOrDefault(x => x.NameEn == mapper.department_e || x.NameTh == mapper.department_t)?.DivisionId;
                        update.ReportToEmpCode = _context.MSTEmployees.Where(x => x.EmployeeCode == mapper.codeHead).FirstOrDefault()?.EmployeeId.ToString() ?? null;
                        update.ModifiedBy = "SYSTEM";
                        update.ModifiedDate = DateTime.Now;
                        update.IsActive = true;
                        _context.SubmitChanges();
                        Log("Update Employee: " + update.EmployeeCode);
                    }

                    foreach (var mapper in inserts)
                    {
                        var insertModel = new MSTEmployee();

                        insertModel.Username = !string.IsNullOrEmpty(mapper.CODNATNL) && mapper.CODNATNL == "01" ? "CN" + mapper.CODEMPID : mapper.CODEMPID;
                        insertModel.EmployeeCode = mapper.CODEMPID;
                        insertModel.NameTh = mapper.NAMEMPT;
                        insertModel.NameEn = mapper.NAMEMPE;
                        insertModel.Email = mapper.EMAIL;
                        insertModel.PositionId = _context.MSTPositions.FirstOrDefault(x => x.NameEn == mapper.NAMPOS || x.NameTh == mapper.NAMPOS)?.PositionId;
                        insertModel.DepartmentId = _context.MSTDepartments.FirstOrDefault(x => x.NameEn == mapper.NAMCENTENG || x.NameTh == mapper.NAMCENTHA)?.DepartmentId;
                        insertModel.DivisionId = _context.MSTDivisions.FirstOrDefault(x => x.NameEn == mapper.department_e || x.NameTh == mapper.department_t)?.DivisionId;
                        insertModel.ReportToEmpCode = _context.MSTEmployees.Where(x => x.EmployeeCode == mapper.codeHead).FirstOrDefault()?.EmployeeId.ToString() ?? null;
                        insertModel.Lang = "EN";
                        insertModel.AccountId = 1;
                        insertModel.ADTitle = string.Empty;
                        insertModel.ModifiedBy = "SYSTEM";
                        insertModel.ModifiedDate = DateTime.Now;
                        insertModel.CreatedBy = "SYSTEM";
                        insertModel.CreatedDate = DateTime.Now;
                        insertModel.IsActive = true;
                        _context.MSTEmployees.InsertOnSubmit(insertModel);
                        _context.SubmitChanges();
                        Log("Insert Employee: " + insertModel.EmployeeCode);
                    }
                    //tran.Commit();
                }
                else
                {
                    Log("ConnectStore fialed: " + viewAllEA);
                }
                Log("================= End =================");
            }
            catch (Exception ex)
            {
                //tran.Rollback();
                Console.WriteLine(ex.ToString());
                Thread.Sleep(100000);
            }
        }
        public static List<T> ExcuteStoreQueryListAsync<T>(string store)
        {
            SqlConnection sqlConnection = new SqlConnection(_Connection);
            try
            {
                sqlConnection.Open();

                SqlCommand command = new SqlCommand(store, sqlConnection);
                command.CommandType = CommandType.StoredProcedure;

                SqlDataReader dr = command.ExecuteReader();
                List<T> list = new List<T>();
                T obj = default;
                while (dr.Read())
                {
                    obj = Activator.CreateInstance<T>();
                    foreach (PropertyInfo prop in obj.GetType().GetProperties())
                    {
                        if (!object.Equals(dr[prop.Name], DBNull.Value))
                        {
                            prop.SetValue(obj, Convert.ChangeType(dr[prop.Name], prop.PropertyType), null);
                        }
                    }
                    list.Add(obj);
                }
                return list;
            }
            catch (Exception ex)
            {
                sqlConnection.Close();
                throw ex;
            }
            finally { sqlConnection.Close(); }
        }
    }
}
