using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using TMPro;

public class FirebaseManager : MonoBehaviour
{
    #region Variables
    [Header("Firebase")]
    public DependencyStatus dependencyStatus;
    public FirebaseAuth auth;
    public FirebaseUser user;
    public DatabaseReference db_ref;

    [Header("Login")]
    public TMP_InputField emailLoginField;
    public TMP_InputField passwordLoginField;
    public TMP_Text warningLoginText;
    public TMP_Text confirmLoginText;

    [Header("Register")]
    public TMP_InputField usernameRegisterField;
    public TMP_InputField emailRegisterField;
    public TMP_InputField passwordRegisterField;
    public TMP_InputField passwordRegisterVerifyField;
    public TMP_Text warningRegisterText;

    [Header("User")]
    public float balance;
    public TMP_InputField transferValueField;
    public TMP_InputField transferUidField;
    public TMP_InputField addValueField;

    [Header("Family")]
    public string family_name;
    public string family_code;
    public TMP_InputField familyCodeField;
    public bool isChild;
    #endregion

    #region Initialisation
    void Awake()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                InitializeFirebase();
            }
            else
            {
                Debug.LogError("Could not resolve all Firebase dependencies: " + dependencyStatus);
            }
        });

    }

    private void InitializeFirebase()
    {
        Debug.Log("Setting up Firebase Auth");
        auth = FirebaseAuth.DefaultInstance;
        db_ref = FirebaseDatabase.GetInstance("https://doyle-s-bank-default-rtdb.asia-southeast1.firebasedatabase.app").RootReference;
    }
    #endregion

    #region Buttons

    //Function for the login button
    public void LoginButton()
    {
        //Call the login coroutine passing the email and password
        StartCoroutine(Login(emailLoginField.text, passwordLoginField.text));
    }
    //Function for the register button
    public void RegisterButton()
    {
        //Call the register coroutine passing the email, password, and username
        StartCoroutine(Register(emailRegisterField.text, passwordRegisterField.text, usernameRegisterField.text));
    }

    public void TransferValueButton()
    {
        TransferValue(transferUidField.text, int.Parse(transferValueField.text));
    }

    public void AddValueButton()
    {
        if (!isChild)
        {
            AddValue(int.Parse(addValueField.text));
        }
    }
    #endregion

    #region Authentication
    private IEnumerator Login(string _email, string _password)
    {
        //Call the Firebase auth signin function passing the email and password
        var LoginTask = auth.SignInWithEmailAndPasswordAsync(_email, _password);
        //Wait until the task completes
        yield return new WaitUntil(predicate: () => LoginTask.IsCompleted);

        if (LoginTask.Exception != null)
        {
            //If there are errors handle them
            Debug.LogWarning(message: $"Failed to register task with {LoginTask.Exception}");
            FirebaseException firebaseEx = LoginTask.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            string message = "Login Failed!";
            switch (errorCode)
            {
                case AuthError.MissingEmail:
                    message = "Missing Email";
                    break;
                case AuthError.MissingPassword:
                    message = "Missing Password";
                    break;
                case AuthError.WrongPassword:
                    message = "Wrong Password";
                    break;
                case AuthError.InvalidEmail:
                    message = "Invalid Email";
                    break;
                case AuthError.UserNotFound:
                    message = "Account does not exist";
                    break;
            }
            warningLoginText.text = message;
        }
        else
        {
            //User is now logged in
            //Now get the result
            user = LoginTask.Result;
            Debug.LogFormat("User signed in successfully: {0} ({1})", user.DisplayName, user.Email);
            warningLoginText.text = "";
            confirmLoginText.text = "Logged In";

            yield return new WaitForSeconds(2);
            UIManager.instance.UserDataScreen();
            UpdateBalance();
        }
    }

    private IEnumerator Register(string _email, string _password, string _username)
    {
        if (_username == "")
        {
            //If the username field is blank show a warning
            warningRegisterText.text = "Missing Username";
        }
        else if (passwordRegisterField.text != passwordRegisterVerifyField.text)
        {
            //If the password does not match show a warning
            warningRegisterText.text = "Password Does Not Match!";
        }
        else
        {
            //Call the Firebase auth signin function passing the email and password
            var RegisterTask = auth.CreateUserWithEmailAndPasswordAsync(_email, _password);
            //Wait until the task completes
            yield return new WaitUntil(predicate: () => RegisterTask.IsCompleted);

            if (RegisterTask.Exception != null)
            {
                //If there are errors handle them
                Debug.LogWarning(message: $"Failed to register task with {RegisterTask.Exception}");
                FirebaseException firebaseEx = RegisterTask.Exception.GetBaseException() as FirebaseException;
                AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

                string message = "Register Failed!";
                switch (errorCode)
                {
                    case AuthError.MissingEmail:
                        message = "Missing Email";
                        break;
                    case AuthError.MissingPassword:
                        message = "Missing Password";
                        break;
                    case AuthError.WeakPassword:
                        message = "Weak Password";
                        break;
                    case AuthError.EmailAlreadyInUse:
                        message = "Email Already In Use";
                        break;
                }
                warningRegisterText.text = message;
            }
            else
            {
                //User has now been created
                //Now get the result
                user = RegisterTask.Result;

                if (user != null)
                {
                    //Create a user profile and set the username
                    UserProfile profile = new UserProfile { DisplayName = _username };
                    LoadBalance();

                    //Call the Firebase auth update user profile function passing the profile with the username
                    var ProfileTask = user.UpdateUserProfileAsync(profile);
                    //Wait until the task completes
                    yield return new WaitUntil(predicate: () => ProfileTask.IsCompleted);

                    if (ProfileTask.Exception != null)
                    {
                        //If there are errors handle them
                        Debug.LogWarning(message: $"Failed to register task with {ProfileTask.Exception}");
                        FirebaseException firebaseEx = ProfileTask.Exception.GetBaseException() as FirebaseException;
                        AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
                        warningRegisterText.text = "Username Set Failed!";
                    }
                    else
                    {
                        //Username is now set
                        //Now return to login screen
                        UIManager.instance.LoginScreen();
                        warningRegisterText.text = "";
                    }
                }
            }
        }
    }
    #endregion

    #region Database Updates
    void LoadBalance()
	{
        db_ref.Child("users").Child(user.UserId).Child("balance").SetValueAsync(balance).ContinueWith(task => {
            if (task.IsFaulted)
            {
                //log error
                Debug.LogError("could not get asynced value : value");
            }
            else if (task.IsCompleted)
            {
                // task completed
            }
        });
    }
    void UpdateBalance()
    {
        db_ref.Child("users").Child(user.UserId).Child("balance").GetValueAsync().ContinueWith(task => {
            if (task.IsFaulted)
            {
                //log error
                Debug.LogError("could not get asynced value : value");
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                float data = int.Parse(snapshot.Value.ToString());
                if (data > balance)
                {
                    balance = data;
                }
                else if (data < balance)
                {
                    db_ref.Child("users").Child(user.UserId).Child("balance").SetValueAsync(balance).ContinueWith(task => {
                        if (task.IsFaulted)
                        {
                            //log error
                            Debug.LogError("could not get asynced value : value");
                        }
                        else if (task.IsCompleted)
                        {
                            // task completed
                        }
                    });
                }
                else
                {
                    // balance in database is equal to balance in unity
                }
            }
        });
    }
    #endregion

    #region Balance/Value Functions
    void TransferValue(string _uid, float _value)
    {
        if (_value > 0 && _uid != user.UserId)
        {
            balance -= _value;
            UpdateBalance();
            // gets the balance of targeted user
            db_ref.Child("users").Child(_uid).Child("balance").GetValueAsync().ContinueWith(task => {
                if (task.IsFaulted)
                {
                    //log error
                    Debug.LogError("could not get asynced value : balance");
                }
                else if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    // adds value to original balance
                    float newBalance = int.Parse(snapshot.Value.ToString());
                    newBalance += _value;
                    // sets value of balance to newly calculated one
                    db_ref.Child("users").Child(_uid).Child("balance").SetValueAsync(newBalance);
                }
            });
        }

    }

    void AddValue(float _value)
    {
        balance += _value;
        UpdateBalance();
        db_ref.Child("users").Child(user.UserId).Child("balance").GetValueAsync().ContinueWith(task => {
            if (task.IsFaulted)
            {
                //log error
                Debug.LogError("could not get asynced value : balance");
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                // adds value to original balance
                float newBalance = int.Parse(snapshot.Value.ToString());
                newBalance += _value;
                // sets value of balance to newly calculated one
                db_ref.Child("users").Child(user.UserId).Child("balance").SetValueAsync(newBalance);
            }
        });
    }
    #endregion

    #region Family Functions
    void GetFamilyChildCount(string _familyCode)
    {
        db_ref.Child("users").Child(_familyCode).GetValueAsync().ContinueWith(task => {
            if (task.IsFaulted)
            {
                Debug.LogError("task was faulted");
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                long no_children = snapshot.ChildrenCount;
            }
        });
    }

    int CreateFamily()
    {
        int code = 1234536; // make a random 5 digit code to represent family
        return code;
    }
    #endregion
}