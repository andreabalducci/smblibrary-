
namespace SMBLibrary
{
    public enum NTStatus : uint
    {
        STATUS_SUCCESS = 0x00000000,
        STATUS_NOT_IMPLEMENTED = 0xC0000002,
        STATUS_INVALID_HANDLE = 0xC0000008,
        STATUS_INVALID_PARAMETER = 0xC000000D,
        STATUS_NO_SUCH_DEVICE = 0xC000000E,
        STATUS_NO_SUCH_FILE = 0xC000000F,
        STATUS_MORE_PROCESSING_REQUIRED = 0xC0000016,
        STATUS_ACCESS_DENIED = 0xC0000022, // The user is not authorized to access the resource.
        STATUS_OBJECT_NAME_INVALID = 0xC0000033,
        STATUS_OBJECT_NAME_NOT_FOUND = 0xC0000034,
        STATUS_OBJECT_NAME_COLLISION = 0xC0000035, // The file already exists
        STATUS_OBJECT_PATH_INVALID = 0xC0000039,
        STATUS_OBJECT_PATH_NOT_FOUND = 0xC000003A, // The share path does not reference a valid resource.
        STATUS_OBJECT_PATH_SYNTAX_BAD = 0xC000003B,
        STATUS_DATA_ERROR = 0xC000003E, // IO error
        STATUS_SHARING_VIOLATION = 0xC0000043,
        STATUS_FILE_LOCK_CONFLICT = 0xC0000054,
        STATUS_LOGON_FAILURE = 0xC000006D, // Authentication failure.
        STATUS_ACCOUNT_RESTRICTION = 0xC000006E, // The user has an empty password, which is not allowed
        STATUS_DISK_FULL = 0xC000007F,
        STATUS_MEDIA_WRITE_PROTECTED = 0xC00000A2,
        STATUS_FILE_IS_A_DIRECTORY = 0xC00000BA,
        STATUS_CANNOT_DELETE = 0xC0000121,
        
        STATUS_INVALID_SMB = 0x00010002,     // CIFS/SMB1: A corrupt or invalid SMB request was received
        STATUS_SMB_BAD_COMMAND = 0x00160002, // CIFS/SMB1: An unknown SMB command code was received by the server
        STATUS_SMB_BAD_FID = 0x00060001, // CIFS/SMB1
        STATUS_SMB_BAD_TID = 0x00050002, // CIFS/SMB1
    }
}
