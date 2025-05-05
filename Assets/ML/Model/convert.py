import tensorflow as tf
import os
import sys

# Load the model
model_path = "object_classifier.h5"
model = tf.keras.models.load_model(model_path)

# Make sure we have the right versions of tf2onnx
try:
    import tf2onnx
except ImportError:
    print("Installing tf2onnx...")
    import subprocess
    subprocess.check_call([sys.executable, "-m", "pip", "install", "--upgrade", "tf2onnx"])
    import tf2onnx

# First, save as a TensorFlow SavedModel (which tends to be more compatible with converters)
saved_model_path = "./saved_model"
tf.saved_model.save(model, saved_model_path)
print(f"Model saved as TensorFlow SavedModel to {saved_model_path}")

# Then convert the SavedModel to ONNX
output_path = "object_classifier.onnx"
cmd = f"python -m tf2onnx.convert --saved-model {saved_model_path} --output {output_path} --opset 13"
print(f"Running conversion command: {cmd}")
os.system(cmd)

print(f"Check if the ONNX file was created: {os.path.exists(output_path)}")