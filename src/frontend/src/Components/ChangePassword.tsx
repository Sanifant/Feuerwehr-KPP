
import { useParams } from "react-router-dom";


interface ResetRequest {
    token: string;
    username: string;
    newPassword: string;
}

export function ChangePassword() {
    let { token } = useParams<{ token: string }>();

    return (
        <div>
            <p>Hello world! Token: {token}</p>

            <form>
                <label>
                    User Name:
                    <input type="email" required />
                </label>
                <label>
                    Passwort:
                    <input type="password" required />
                </label>
                <label>
                    Passwort wiederholen:
                    <input type="password" required />
                </label>

                <button>
                    Ändern
                </button>
            </form>

        </div>
    );
}