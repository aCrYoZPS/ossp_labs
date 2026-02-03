#!/bin/bash

player="X"
turn_count=0
game_on=true
declare -a board=(" " " " " " " " " " " " " " " " " " " ")

RED=$(tput setaf 1)
GREEN=$(tput setaf 2)
BLUE=$(tput setaf 4)
YELLOW=$(tput setaf 3)
RESET=$(tput sgr0)

strip_colors() {
    echo "$1" | sed -e 's/\x1b\[[0-9;]*m//g' -e 's/\x1b([A-Z]//g'
}

get_player_color() {
    if [ "$player" == "X" ]; then
        echo "$GREEN"
    else
        echo "$RED"
    fi
}

save_board() {
    timestamp=$(date +"%Y-%m-%d_%H-%M-%S")
    filename="screenshot_${timestamp}.txt"
    {
        echo "Current Board"
        echo "-------------"
        printf " %s | %s | %s \n" \
            "$(strip_colors "${board[1]}")" \
            "$(strip_colors "${board[2]}")" \
            "$(strip_colors "${board[3]}")"
        echo "---+---+---"
        printf " %s | %s | %s \n" \
            "$(strip_colors "${board[4]}")" \
            "$(strip_colors "${board[5]}")" \
            "$(strip_colors "${board[6]}")"
        echo "---+---+---"
        printf " %s | %s | %s \n\n" \
            "$(strip_colors "${board[7]}")" \
            "$(strip_colors "${board[8]}")" \
            "$(strip_colors "${board[9]}")"
        echo "Player ${player}'s turn."
    } > "$filename"

    echo "$filename"
}

draw_board() {
    clear
    printf "${YELLOW}=== TIC-TAC-TOE ===${RESET}\n"
    printf "Type the number (1-9) to place your mark. Type 'S' to save the current board\n\n"
    
    printf "  Reference Map      Current Board\n"
    printf "  -------------      -------------\n"
    printf "    1 | 2 | 3          %s | %s | %s \n" "${board[1]}" "${board[2]}" "${board[3]}"
    printf "   ---+---+---        ---+---+---\n"
    printf "    4 | 5 | 6          %s | %s | %s \n" "${board[4]}" "${board[5]}" "${board[6]}"
    printf "   ---+---+---        ---+---+---\n"
    printf "    7 | 8 | 9          %s | %s | %s \n\n" "${board[7]}" "${board[8]}" "${board[9]}"
}

check_winner() {
    if [[ "${board[1]}" != " " ]] && [[ "${board[1]}" == "${board[2]}" ]] && [[ "${board[2]}" == "${board[3]}" ]]; then return 0; fi
    if [[ "${board[4]}" != " " ]] && [[ "${board[4]}" == "${board[5]}" ]] && [[ "${board[5]}" == "${board[6]}" ]]; then return 0; fi
    if [[ "${board[7]}" != " " ]] && [[ "${board[7]}" == "${board[8]}" ]] && [[ "${board[8]}" == "${board[9]}" ]]; then return 0; fi
    
    if [[ "${board[1]}" != " " ]] && [[ "${board[1]}" == "${board[4]}" ]] && [[ "${board[4]}" == "${board[7]}" ]]; then return 0; fi
    if [[ "${board[2]}" != " " ]] && [[ "${board[2]}" == "${board[5]}" ]] && [[ "${board[5]}" == "${board[8]}" ]]; then return 0; fi
    if [[ "${board[3]}" != " " ]] && [[ "${board[3]}" == "${board[6]}" ]] && [[ "${board[6]}" == "${board[9]}" ]]; then return 0; fi

    if [[ "${board[1]}" != " " ]] && [[ "${board[1]}" == "${board[5]}" ]] && [[ "${board[5]}" == "${board[9]}" ]]; then return 0; fi
    if [[ "${board[3]}" != " " ]] && [[ "${board[3]}" == "${board[5]}" ]] && [[ "${board[5]}" == "${board[7]}" ]]; then return 0; fi

    return 1
}

while $game_on; do
    draw_board
    
    color=$(get_player_color "$player")
    printf "Player ${color}$player${RESET}'s turn.\n"
    printf "Enter cell (1-9) or command: "
    read choice
    
    if [ $choice == "S" ]; then
        filename=$(save_board)
        printf "${BLUE}Screenshot saved to ${filename}.${RESET}\n"
        sleep 3
        continue
    fi

    if [[ ! $choice =~ ^[1-9]$ ]]; then
        printf "${RED}Invalid input! Please enter a number between 1 and 9.${RESET}\n"
        sleep 1
        continue
    fi

    if [[ "${board[$choice]}" != " " ]]; then
        printf "${RED}Cell is already taken! Try again.${RESET}\n"
        sleep 1
        continue
    fi

    if [[ "$player" == "X" ]]; then
        board[$choice]="${GREEN}X${RESET}"
    else
        board[$choice]="${RED}O${RESET}"
    fi

    if check_winner; then
        draw_board
        printf "${GREEN}Congratulations! Player $player wins!${RESET}\n"
        game_on=false
        exit 0
    fi

    ((turn_count++))
    if [[ $turn_count -eq 9 ]]; then
        draw_board
        printf "${YELLOW}It's a Draw!${RESET}\n"
        game_on=false
        exit 0
    fi

    if [[ "$player" == "X" ]]; then
        player="O"
    else
        player="X"
    fi
done
