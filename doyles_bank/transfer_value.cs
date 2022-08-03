/* this program shows an example of how a transfer money function would work with firebase authentication and database in unity, it 
mostly displays how the database would work but uses the authentication of users in order to function. TransferValue function takes in
a value and a user id in order to find the balance of the target user, it then writes new data to that balance in order to "transfer" it 
a certain value. This script would be used in a certain banking app*/

DatabaseReference db_ref;
FirebaseUser user;
FirebaseAuth auth;
FirebaseApp app;

void Awake(){
    Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
        var dependencyStatus = task.Result;
        if (dependencyStatus == Firebase.DependencyStatus.Available) {
            // Create and hold a reference to your FirebaseApp,
            // where app is a Firebase.FirebaseApp property of your application class.
        app = Firebase.FirebaseApp.DefaultInstance;

            // Set a flag here to indicate whether Firebase is ready to use by your app.
        } 
        else {
            UnityEngine.Debug.LogError(System.String.Format(
            "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
            // Firebase Unity SDK is not safe to use here.
        }
    });
}

void InititialiseFirebase(){
    db_ref = FirebaseDatabase.DefaultInstance.RootReference;
    db_ref.Child("users").Child(user.GetUid()).Child("balance").ValueChanged += HandleUpdateBalance;//this event is played whenever the balance value is changed
    auth = FirebaseAuth.DefaultInstance; 
    user = FirebaseAuth.getInstance().getCurrentUser();//gets current user logged in with firebase authentication
}

void Start(){
    InititialiseFirebase();
}

void HandleUpdateBalance(object sender, ValueChangedEventArgs args){
    DataSnapshot snapshot = args.snapshot;
}

void FindUidByEmail(string _email){
    UserRecord targetUserRecord = await FirebaseAdmin.Auth.GetUserByEmailAsync(_email);
    string uid = targetUserRecord.Uid;
    return uid;
}

void UpdateBalance(int _balance){
    db_ref.Child("users").Child(user.Uid).Child("balance").SetValueAsync(_balance).ContinueWith(task =>{
        if(task.isFaulted){
            //log error
            Debug.LogError("could not get asynced value : value")
        }
        else if(task.isCompleted){
            // task completed
        }
    });
}

void TransferValue(string _uid, int _value){
    // gets the balance of targeted user
    db_ref.Child("users").Child(_uid).Child("balance").GetValueAsync().ContinueWith(task =>{
        if(task.isFaulted){
            //log error
            Debug.LogError("could not get asynced value : balance")
        }
        else if(task.isCompleted){
            DataSnapshot snapshot = task.Result;
            // adds value to original balance
            int newBalance = int.Parse(Convert.ToString(snapshot.value));
            newBalance += _value;
            // sets value of balance to newly calculated one
            db_ref.Child("users").Child(_uid).Child("balance").SetValueAsync(newBalance);
        }
    });
}
