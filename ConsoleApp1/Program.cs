using System;
using System.Management;

public class DeviceWatcher
{
    public void StartWatching()
    {
        // Monitor for new USB storage devices
        ManagementEventWatcher usbWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'"));
        usbWatcher.EventArrived += new EventArrivedEventHandler(DeviceEventArrived);
        usbWatcher.Start();

        // Monitor for new disk drives being added
        ManagementEventWatcher diskWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_DiskDrive'"));
        diskWatcher.EventArrived += new EventArrivedEventHandler(DeviceEventArrived);
        diskWatcher.Start();

        Console.WriteLine("Listening for new devices. Press any key to exit.");
        Console.ReadKey();

        usbWatcher.Stop();
        diskWatcher.Stop();
    }

    private void DeviceEventArrived(object sender, EventArrivedEventArgs e)
    {
        ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
        string deviceID = instance["DeviceID"].ToString();
        string description = instance["Description"].ToString();

        Console.WriteLine($"New device detected: {description} with DeviceID {deviceID}");
        if (description.ToLower().Contains("disk"))
        {
            // Additional logic here to handle disk drives specifically
            Console.WriteLine($"Disk drive detected: {deviceID}. Checking for further details...");
            // Optionally, add code here to query for more details about the disk drive
            FindDriveLetter(deviceID);
        }
    }

    private void FindDriveLetter(string deviceId)
    {
        // Remove backslashes for WMI query compatibility
        string modifiedDeviceId = deviceId.Replace("\\", "\\\\");

        // Query to map Win32_DiskDrive to Win32_DiskPartition
        string query = $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{deviceId}'}} WHERE AssocClass = Win32_DiskDriveToDiskPartition";
        ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);

        foreach (ManagementObject diskPartition in searcher.Get())
        {
            // Query to map Win32_DiskPartition to Win32_LogicalDisk
            string partitionQuery = $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{diskPartition["DeviceID"]}'}} WHERE AssocClass = Win32_LogicalDiskToPartition";
            ManagementObjectSearcher partitionSearcher = new ManagementObjectSearcher(partitionQuery);

            foreach (ManagementObject logicalDisk in partitionSearcher.Get())
            {
                Console.WriteLine($"Drive letter for {deviceId}: {logicalDisk["DeviceID"]}");
            }
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        DeviceWatcher watcher = new DeviceWatcher();
        watcher.StartWatching();
    }
}
