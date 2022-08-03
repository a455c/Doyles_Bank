/* this script is used in unity connected with the firebase database in order to change the data located with the key "value"
to a different integer. there are outputting functions created that can be used to update application ui. */

DatabaseReference db_ref;//store a location in the database

void InititialiseFirebase(){
    db_ref = FirebaseDatabase.DefaultInstance.RootReference;// set a location in database where values can be written to
    FirebaseDatabase.DefaultInstance.GetReference("value").ValueChanged += HandleValueChanged;
    // Handle value changed is an event run when the database changes a value
}

void Start(){
    InititialiseFirebase();
}

// function used to get new values that can be used to update ui
void HandleValueChanged(object sender, ValueChangedEventArgs args){
    DataSnapshot data = args.DataSnapshot;
    //update ui with this data 
} 

void UpdateScore(){
    FirebaseDatabase.DefaultInstance.GetReference("value").GetValueAsync().ContinueWith(task => {
        if(task.isFaulted){
            //log error
            Debug.LogError("could not get asynced value : value")
        }
        else if(task.isCompleted){
            DataSnapshot data = task.Result;// grab result and store it
            int value = data.value;//int value = int.Parse(Convert.ToString(data.value)); allow the data to be readable
            value += 5;// add increment or transfered amount
            db_ref.Child("value").SetValueAsync(value);// overwrites data to database in location of reference's child -> value 
        }
    }); // getvalueasync is a "task" requiring a different format of completetion
}