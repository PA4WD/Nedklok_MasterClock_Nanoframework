#!/bin/bash

# Sets up the Git repository hooks
echo "setup the Git repository hooks";
# Precommit Hooks
if test -f ".git/hooks/pre-commit"; then
	rm .git/hooks/pre-commit
 fi

tee -a .git/hooks/pre-commit << EOF
# Don't let a commit be made to the credentials config file without the permission to do so
git diff --cached --name-only | if grep --quiet "WifiCredentials.cs"
then
	if test -f "Nedklok_MasterClock_Nanoframework/commit-my-credentials"; then
		rm -rf "Nedklok_MasterClock_Nanoframework/commit-my-credentials";
		exit 0;
	else
		echo "You are making changes to a credentials file...";
		echo "Either: ";
		echo "  1 - Do not commit the WifiCredentials.cs file, OR";
		echo "  2 - Create a blank file called 'commit-my-credentials' file in the same directory"
		echo "";
		echo "This commit has not been completed.";
		exit 1;
	fi
fi
EOF
chmod gou+x .git/hooks/pre-commit;

# Tell the user we're done
echo "";
echo "";
echo "Mission Complete.";
