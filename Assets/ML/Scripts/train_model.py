import os
import sys
import numpy as np
import tensorflow as tf
from tensorflow.keras.models import Sequential
from tensorflow.keras.layers import Conv2D, MaxPooling2D, Flatten, Dense, Dropout
from tensorflow.keras.preprocessing.image import ImageDataGenerator
from tensorflow.keras.optimizers import Adam
from tensorflow.keras.callbacks import ModelCheckpoint, EarlyStopping
import tensorflow.keras.backend as K
from sklearn.metrics import mean_squared_error, mean_absolute_error, r2_score

# Set random seed for reproducibility
tf.random.set_seed(42)
np.random.seed(42)

# Configuration
TRAINING_DIR = "Assets/ML/Training"
IMAGE_SIZE = (224, 224)
BATCH_SIZE = 32
EPOCHS = 50
OUTPUT_MODEL_H5 = "Assets/ML/Model/object_classifier.h5"

# Custom metrics functions
def rmse(y_true, y_pred):
    return K.sqrt(K.mean(K.square(y_true - y_pred)))

def r_squared(y_true, y_pred):
    SS_res = K.sum(K.square(y_true - y_pred))
    SS_tot = K.sum(K.square(y_true - K.mean(y_true)))
    return 1 - SS_res/(SS_tot + K.epsilon())

def create_model(num_classes):
    """Create a simple CNN model for object classification."""
    model = Sequential()
    model.add(Conv2D(32, (3, 3), activation='relu', padding='same', input_shape=(*IMAGE_SIZE, 3)))
    model.add(MaxPooling2D(2, 2))
    model.add(Conv2D(64, (3, 3), activation='relu', padding='same'))
    model.add(MaxPooling2D(2, 2))
    model.add(Conv2D(128, (3, 3), activation='relu', padding='same'))
    model.add(MaxPooling2D(2, 2))
    model.add(Conv2D(128, (3, 3), activation='relu', padding='same'))
    model.add(MaxPooling2D(2, 2))
    model.add(Flatten())
    model.add(Dense(512, activation='relu'))
    model.add(Dropout(0.5))
    model.add(Dense(num_classes, activation='softmax'))
    
    model.compile(
        optimizer=Adam(learning_rate=0.0001),
        loss='categorical_crossentropy',
        metrics=['accuracy', rmse, 'mae', r_squared]
    )
    
    return model

def train_model():
    """Train the object classification model."""
    # Check if training directory exists
    if not os.path.exists(TRAINING_DIR):
        print(f"Error: Training directory '{TRAINING_DIR}' not found!")
        return
    
    # Get class names from subdirectories
    class_names = [d for d in os.listdir(TRAINING_DIR) 
                   if os.path.isdir(os.path.join(TRAINING_DIR, d))]
    num_classes = len(class_names)
    
    if num_classes == 0:
        print("Error: No classes found in training directory!")
        return
    
    print(f"Found {num_classes} classes: {class_names}")
    
    # Create data generators with augmentation for training
    train_datagen = ImageDataGenerator(
        rescale=1./255,
        validation_split=0.2,
        rotation_range=20,
        width_shift_range=0.2,
        height_shift_range=0.2,
        shear_range=0.2,
        zoom_range=0.2,
        horizontal_flip=True,
        fill_mode='nearest'
    )
    
    # Create train and validation generators
    train_generator = train_datagen.flow_from_directory(
        TRAINING_DIR,
        target_size=IMAGE_SIZE,
        batch_size=BATCH_SIZE,
        class_mode='categorical',
        subset='training',
        shuffle=True
    )
    
    validation_generator = train_datagen.flow_from_directory(
        TRAINING_DIR,
        target_size=IMAGE_SIZE,
        batch_size=BATCH_SIZE,
        class_mode='categorical',
        subset='validation',
        shuffle=False
    )
    
    # Create model
    model = create_model(num_classes)
    
    # Create output directory if it doesn't exist
    os.makedirs(os.path.dirname(OUTPUT_MODEL_H5), exist_ok=True)
    
    # Setup callbacks
    checkpoint = ModelCheckpoint(
        'model_checkpoint.h5',
        monitor='val_accuracy',
        save_best_only=True,
        mode='max',
        verbose=1
    )
    
    early_stopping = EarlyStopping(
        monitor='val_accuracy',
        patience=10,
        restore_best_weights=True,
        mode='max',
        verbose=1
    )
    
    # Train the model
    history = model.fit(
        train_generator,
        steps_per_epoch=train_generator.samples // BATCH_SIZE,
        validation_data=validation_generator,
        validation_steps=validation_generator.samples // BATCH_SIZE,
        epochs=EPOCHS,
        callbacks=[checkpoint, early_stopping]
    )
    
    # Save model in H5 format
    model.save(OUTPUT_MODEL_H5)
    print(f"Model saved in H5 format: {OUTPUT_MODEL_H5}")
    
    # Calculate additional metrics on validation data
    print("\nEvaluating model on validation data:")
    results = model.evaluate(validation_generator)
    
    # Print detailed metrics
    metric_names = ['loss', 'accuracy', 'rmse', 'mae', 'r_squared']
    for name, value in zip(metric_names, results):
        print(f"{name}: {value:.4f}")
    
    # Generate predictions for additional sklearn metrics
    steps = validation_generator.samples // BATCH_SIZE + (1 if validation_generator.samples % BATCH_SIZE > 0 else 0)
    predictions = model.predict(validation_generator, steps=steps)
    
    # Get actual labels
    validation_generator.reset()
    y_true = []
    for i in range(steps):
        batch_x, batch_y = next(validation_generator)
        y_true.extend(batch_y)
        if len(y_true) >= validation_generator.samples:
            break
    
    # Clip to actual number of samples
    y_true = np.array(y_true[:validation_generator.samples])
    predictions = predictions[:validation_generator.samples]
    
    # Calculate additional sklearn metrics
    # For multi-class classification, we need to handle these differently
    # Convert one-hot encoded to class indices for easier comparison
    y_true_classes = np.argmax(y_true, axis=1)
    y_pred_classes = np.argmax(predictions, axis=1)
    
    # Calculate confusion matrix and classification report
    from sklearn.metrics import confusion_matrix, classification_report
    
    cm = confusion_matrix(y_true_classes, y_pred_classes)
    print("\nConfusion Matrix:")
    print(cm)
    
    print("\nClassification Report:")
    print(classification_report(y_true_classes, y_pred_classes, 
                              target_names=list(validation_generator.class_indices.keys())))
    
    # Return model for potential further use
    return model, history

if __name__ == "__main__":
    train_model()