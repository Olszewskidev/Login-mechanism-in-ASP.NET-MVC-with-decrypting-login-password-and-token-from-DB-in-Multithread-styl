using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class LoginController : Controller
    {
        private int RanNum { get; set; }
        private int RandomNumberToToken()
        {
            Random ran = new Random();
            return ran.Next(1, 5); // I have only 5 tokens in DB tabel, so I have to take random number from 1 to 5.
        }
        public ActionResult Login()
        {
            RanNum = RandomNumberToToken();
            ViewBag.RanNum = RanNum;
            return View();
        }
        [HttpPost]
        public ActionResult Login(UserInfo UsI, string ReturnUrl)
        {
            if (IsCorrect(UsI))
            {
                FormsAuthentication.SetAuthCookie(UsI.Name, false);
                return Redirect(ReturnUrl);
            }
            else
                return View();
        }
        private bool IsCorrect(UserInfo UsI)
        {
            string ELogin;
            string EPassword;
            string EToken; 
            //Get encrypted data from DB table (Login, Password, Token)
            using (HelloBlogEntities db=new HelloBlogEntities())
            {      
                var DataFromDB = db.LoginData.Where(r => r.Id == 1).FirstOrDefault();
                int num = Convert.ToInt32(TempData["TokenNumb"]); // Number of token which was in the View on Login, which was created from RandomNumber on Start View Login.
                EToken = db.TokenCard.Where(r => r.Id == num).FirstOrDefault().Token.ToString();
                ELogin = DataFromDB.Login.ToString();
                EPassword = DataFromDB.Password.ToString();
            }
            //Encrypt Data From DB in multithreading style using Tasks
            Task<string> task1 = Task.Factory.StartNew(() => DecryptMyStuff(ELogin));
            Task<string> task2 = Task.Factory.StartNew(() => DecryptMyStuff(Regex.Replace(EPassword, @"\s+", "")));
            Task<string> task3 = Task.Factory.StartNew(() => DecryptMyStuff(EToken));
            Task.WaitAll();
            //After Tasks is completed results are inicialization to the strings variables
            if (Task.WhenAll().IsCompleted)
            {
                ELogin = task1.Result;
                EPassword= task2.Result;
                EToken = task3.Result;
            }
            //Checking if login, password, token are corrects, and if correct return true
            return (UsI.Name == ELogin && UsI.Password == EPassword && UsI.Token == EToken ? true: false);
        }
        //I have 3 strings variabels to decrypt (Login, Password, Token). The data is encrypeted by 3DES methode, with my special key value
        private string DecryptMyStuff (string EncryptedDataFromDB)
        {
            TripleDESCryptoServiceProvider tr = new TripleDESCryptoServiceProvider();
            ICryptoTransform toArray = tr.CreateEncryptor();
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            UTF8Encoding utf = new UTF8Encoding();
            tr.Key = md5.ComputeHash(utf.GetBytes("**************")); //The key is have to be the same like key used to the encrypted data
            tr.Mode = CipherMode.ECB;
            tr.Padding = PaddingMode.PKCS7;
            ICryptoTransform trans = tr.CreateDecryptor();
            byte[] data = EncryptedDataFromDB.Split('-').Select(b => Convert.ToByte(b, 16)).ToArray();
            EncryptedDataFromDB = utf.GetString(trans.TransformFinalBlock(data, 0, data.Length));
            return EncryptedDataFromDB;
        }
      
    }
}