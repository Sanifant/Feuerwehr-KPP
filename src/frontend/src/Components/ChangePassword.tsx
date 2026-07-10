
import { useState } from "react";
import { useParams } from "react-router-dom";


interface ResetRequest {
    token: string;
    username: string;
    newPassword: string;
}

interface ResetError {
    password?: string;
}

export function ChangePassword() {
    let { token } = useParams<{ token: string }>();

    const [resetRequest, setResetRequest] = useState<ResetRequest>({
        token: token || "",
        username: "",
        newPassword: ""
    });
    const [resetError, setResetError] = useState<ResetError>({});

    return (
        <div>
            <p>Hello world! Token: {token}</p>

            <form>
                <label>
                    User Name:
                    <input
                        type="email"
                        required
                        value={resetRequest.username}
                        onChange={(e) => setResetRequest({ ...resetRequest, username: e.target.value })}
                    />
                </label>
                <label>
                    Passwort:
                    <input
                        type="password"
                        required
                        value={resetRequest.newPassword}
                        onChange={(e) => setResetRequest({ ...resetRequest, newPassword: e.target.value })}
                    />
                </label>
                <div>
                    <label htmlFor="passwordRepeat">Passwort wiederholen:</label>
                    <input
                        id="passwordRepeat"
                        type="password"
                        required
                        value={resetRequest.newPassword}
                        onChange={(e) => setResetRequest({ ...resetRequest, newPassword: e.target.value })}
                    />
                    {resetError.password && <span>{resetError.password}</span>}
                </div>
                <button>
                    Ändern
                </button>
            </form>

        </div>
    );
}